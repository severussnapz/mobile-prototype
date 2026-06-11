using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Genesis.AI.Infrastructure.Services;

/// <summary>
/// Assembles prototype/index.html deterministically from fragment files.
/// Fragment directory: prototype/fragments/
///   _shell.html    — document scaffold with GENESIS: markers
///   _styles.css    — all CSS
///   _app.js        — nav/show-hide/form logic
///   data.js        — ALL fictional data as inline constants
///   screen-NN-{slug}.html — one fragment per screen, ordered by NN prefix
///
/// GENESIS markers (load-bearing — do not rename):
///   &lt;!-- GENESIS:STYLES --&gt;   ← _styles.css inlined into &lt;style&gt;
///   &lt;!-- GENESIS:NAV --&gt;      ← nav items auto-generated from screen list
///   &lt;!-- GENESIS:SCREENS --&gt; ← screen fragments concatenated in NN order
///   &lt;!-- GENESIS:DATA --&gt;     ← data.js inlined into &lt;script&gt;
///   &lt;!-- GENESIS:APP --&gt;      ← _app.js inlined into &lt;script&gt;
/// </summary>
public sealed class PrototypeAssemblyService : IPrototypeAssemblyService
{
    private const string FragmentPrefix = "prototype/fragments/";
    private const string OutputPath = "prototype/index.html";
    private const string ShellPath = "prototype/fragments/_shell.html";
    private const string StylesPath = "prototype/fragments/_styles.css";
    private const string AppPath = "prototype/fragments/_app.js";
    private const string DataPath = "prototype/fragments/data.js";

    private const string MarkerStyles = "<!-- GENESIS:STYLES -->";
    private const string MarkerNav = "<!-- GENESIS:NAV -->";
    private const string MarkerScreens = "<!-- GENESIS:SCREENS -->";
    private const string MarkerData = "<!-- GENESIS:DATA -->";
    private const string MarkerApp = "<!-- GENESIS:APP -->";

    private const string PrototypeBanner = "⚠️ PROTOTYPE ONLY — Requirements validation artefact. Not for production use.";

    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PrototypeAssemblyService> _logger;

    public PrototypeAssemblyService(
        IArtefactRepository artefactRepository,
        IArtefactStorageService artefactStorageService,
        TimeProvider timeProvider,
        ILogger<PrototypeAssemblyService> logger)
    {
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _artefactStorageService = artefactStorageService ?? throw new ArgumentNullException(nameof(artefactStorageService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task AssemblePrototypeAsync(Guid projectId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("PrototypeAssembly: starting assembly for project {ProjectId}", projectId);

        // 1. Load all artefacts for the project
        var allArtefacts = await _artefactRepository.GetByProjectIdAsync(projectId, cancellationToken);

        // 2. Load shell — fail closed if missing
        var shellArtefact = allArtefacts.FirstOrDefault(artefact =>
            artefact.FilePath.Equals(ShellPath, StringComparison.OrdinalIgnoreCase));

        if (shellArtefact is null)
        {
            _logger.LogWarning("PrototypeAssembly: SHELL_MISSING — {ShellPath} not found, skipping assembly", ShellPath);
            return;
        }

        var shell = await _artefactStorageService.GetContentAsync(shellArtefact.S3Key, cancellationToken);
        if (shell is null)
        {
            _logger.LogWarning("PrototypeAssembly: shell content could not be retrieved, skipping assembly");
            return;
        }

        // 3. Get screen fragments sorted by NN prefix
        var screenFragments = allArtefacts
            .Where(artefact => artefact.FilePath.StartsWith(FragmentPrefix, StringComparison.OrdinalIgnoreCase)
                && System.IO.Path.GetFileName(artefact.FilePath).StartsWith("screen-", StringComparison.OrdinalIgnoreCase))
            .OrderBy(artefact => ExtractScreenNumber(artefact.FilePath))
            .ToList();

        // 4. Load optional fragments — only fail closed on styles/app/data when screens exist
        var stylesArtefact = allArtefacts.FirstOrDefault(artefact =>
            artefact.FilePath.Equals(StylesPath, StringComparison.OrdinalIgnoreCase));
        var appArtefact = allArtefacts.FirstOrDefault(artefact =>
            artefact.FilePath.Equals(AppPath, StringComparison.OrdinalIgnoreCase));
        var dataArtefact = allArtefacts.FirstOrDefault(artefact =>
            artefact.FilePath.Equals(DataPath, StringComparison.OrdinalIgnoreCase));

        if (screenFragments.Count > 0 && (stylesArtefact is null || appArtefact is null || dataArtefact is null))
        {
            _logger.LogWarning(
                "PrototypeAssembly: screen fragments exist but required fragments missing " +
                "(styles={StylesMissing}, app={AppMissing}, data={DataMissing}), skipping assembly",
                stylesArtefact is null, appArtefact is null, dataArtefact is null);
            return;
        }

        var styles = stylesArtefact is not null
            ? await _artefactStorageService.GetContentAsync(stylesArtefact.S3Key, cancellationToken) ?? string.Empty
            : string.Empty;
        var appJs = appArtefact is not null
            ? await _artefactStorageService.GetContentAsync(appArtefact.S3Key, cancellationToken) ?? string.Empty
            : string.Empty;
        var dataJs = dataArtefact is not null
            ? await _artefactStorageService.GetContentAsync(dataArtefact.S3Key, cancellationToken) ?? string.Empty
            : string.Empty;

        // 5. Build screens HTML and nav
        var screensBuilder = new System.Text.StringBuilder();
        var navBuilder = new System.Text.StringBuilder();

        foreach (var screenArtefact in screenFragments)
        {
            var screenContent = await _artefactStorageService.GetContentAsync(
                screenArtefact.S3Key, cancellationToken);
            if (screenContent is not null)
                screensBuilder.Append(screenContent);

            var navLabel = BuildNavLabel(screenArtefact.FilePath);
            var screenId = BuildScreenId(screenArtefact.FilePath);
            navBuilder.AppendLine(System.Globalization.CultureInfo.InvariantCulture,
                $"<li><a href=\"#{screenId}\" onclick=\"showScreen('{screenId}')\">{navLabel}</a></li>");
        }

        // 6. Replace markers
        var assembled = shell
            .Replace(MarkerStyles, $"<style>\n{styles}\n</style>", StringComparison.Ordinal)
            .Replace(MarkerNav, navBuilder.ToString(), StringComparison.Ordinal)
            .Replace(MarkerScreens, screensBuilder.ToString(), StringComparison.Ordinal)
            .Replace(MarkerData, $"<script>\n{dataJs}\n</script>", StringComparison.Ordinal)
            .Replace(MarkerApp, $"<script>\n{appJs}\n</script>", StringComparison.Ordinal);

        // 7. Validate assembled output — fail closed
        var validationError = ValidateAssembledOutput(assembled);
        if (validationError is not null)
        {
            _logger.LogError(
                "PrototypeAssembly: validation failed — {Reason}. Assembly output NOT persisted.",
                validationError);
            return;
        }

        // 8. Persist as new version of prototype/index.html
        var nextVersion = await _artefactRepository.GetNextVersionForFileAsync(
            projectId, OutputPath, cancellationToken);

        var storageKey = await _artefactStorageService.SaveContentAsync(
            projectId, OutputPath, nextVersion, assembled, "text/html", cancellationToken);

        var outputArtefact = Artefact.CreateS3Artefact(
            projectId,
            nextVersion,
            OutputPath,
            storageKey,
            "text/html",
            System.Text.Encoding.UTF8.GetByteCount(assembled),
            "system-assembly",
            _timeProvider);

        await _artefactRepository.AddAsync(outputArtefact, cancellationToken);
        await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        await _artefactRepository.DeletePreviousVersionsAsync(
            projectId, OutputPath, nextVersion, cancellationToken);

        _logger.LogInformation(
            "PrototypeAssembly: assembled {OutputPath} v{Version} ({Screens} screens, {Bytes} bytes)",
            OutputPath, nextVersion, screenFragments.Count,
            System.Text.Encoding.UTF8.GetByteCount(assembled));
    }

    private static string? ValidateAssembledOutput(string html)
    {
        if (!html.Contains("<script id=\"prototype-metadata\" type=\"application/json\">", StringComparison.OrdinalIgnoreCase))
            return "Missing prototype-metadata script block";

        if (!html.Contains(PrototypeBanner, StringComparison.Ordinal))
            return "Missing prototype banner string";

        // Check no GENESIS markers remain
        foreach (var marker in new[] { MarkerStyles, MarkerNav, MarkerScreens, MarkerData, MarkerApp })
        {
            if (html.Contains(marker, StringComparison.Ordinal))
                return $"Unresolved marker: {marker}";
        }

        // Check no external resources (src= or href= pointing to http/https)
        // Allow anchor hrefs like href="#something"
        if (System.Text.RegularExpressions.Regex.IsMatch(html,
            @"(src|href)\s*=\s*[""']https?://", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            return "External resource reference detected (src/href pointing to http/https)";

        return null;
    }

    private static int ExtractScreenNumber(string filePath)
    {
        var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
        // screen-NN-{slug} → extract NN
        var parts = fileName.Split('-');
        if (parts.Length >= 2 && int.TryParse(parts[1], out var number))
            return number;
        return int.MaxValue;
    }

    private static string BuildNavLabel(string filePath)
    {
        var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
        // screen-01-patient-search → "Patient Search"
        var parts = fileName.Split('-');
        if (parts.Length > 2)
        {
            return string.Join(" ", parts.Skip(2)
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
        }
        return fileName;
    }

    private static string BuildScreenId(string filePath)
    {
        return System.IO.Path.GetFileNameWithoutExtension(filePath);
    }
}
