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
        var allArtefacts = await _artefactRepository.GetByProjectIdAsync(projectId, cancellationToken);

        var shell = await TryLoadShellAsync(allArtefacts, cancellationToken);
        if (shell is null)
        {
            return;
        }

        var screenFragments = GetScreenFragments(allArtefacts);
        var supportingFragments = await TryLoadSupportingFragmentsAsync(allArtefacts, screenFragments.Count, cancellationToken);
        if (supportingFragments is null)
        {
            return;
        }

        var (screensHtml, navHtml) = await BuildScreensAndNavAsync(screenFragments, cancellationToken);
        var assembled = BuildAssembledDocument(shell, supportingFragments.Value, screensHtml, navHtml);

        var validationError = ValidateAssembledOutput(assembled);
        if (validationError is not null)
        {
            _logger.LogError(
                "PrototypeAssembly: validation failed — {Reason}. Assembly output NOT persisted.",
                validationError);
            return;
        }

        await PersistAssembledPrototypeAsync(projectId, assembled, screenFragments.Count, cancellationToken);
    }

    private async Task<string?> TryLoadShellAsync(IReadOnlyList<Artefact> allArtefacts, CancellationToken cancellationToken)
    {
        var shellArtefact = allArtefacts.FirstOrDefault(artefact =>
            artefact.FilePath.Equals(ShellPath, StringComparison.OrdinalIgnoreCase));

        if (shellArtefact is null)
        {
            _logger.LogWarning("PrototypeAssembly: SHELL_MISSING — {ShellPath} not found, skipping assembly", ShellPath);
            return null;
        }

        var shellContent = await _artefactStorageService.GetContentAsync(shellArtefact.S3Key, cancellationToken);
        if (shellContent is null)
        {
            _logger.LogWarning("PrototypeAssembly: shell content could not be retrieved, skipping assembly");
            return null;
        }

        return shellContent;
    }

    private static List<Artefact> GetScreenFragments(IReadOnlyList<Artefact> allArtefacts)
    {
        return allArtefacts
            .Where(artefact => artefact.FilePath.StartsWith(FragmentPrefix, StringComparison.OrdinalIgnoreCase)
                && System.IO.Path.GetFileName(artefact.FilePath).StartsWith("screen-", StringComparison.OrdinalIgnoreCase))
            .GroupBy(artefact => artefact.FilePath, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(artefact => artefact.Version).First())
            .OrderBy(artefact => ExtractScreenNumber(artefact.FilePath))
            .ToList();
    }

    private async Task<(string Styles, string AppJs, string DataJs)?> TryLoadSupportingFragmentsAsync(
        IReadOnlyList<Artefact> allArtefacts,
        int screenCount,
        CancellationToken cancellationToken)
    {
        var latestByFilePath = allArtefacts
            .GroupBy(artefact => artefact.FilePath, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(artefact => artefact.Version).First())
            .ToList();

        var stylesArtefact = latestByFilePath.FirstOrDefault(artefact =>
            artefact.FilePath.Equals(StylesPath, StringComparison.OrdinalIgnoreCase));
        var appArtefact = latestByFilePath.FirstOrDefault(artefact =>
            artefact.FilePath.Equals(AppPath, StringComparison.OrdinalIgnoreCase));
        var dataArtefact = latestByFilePath.FirstOrDefault(artefact =>
            artefact.FilePath.Equals(DataPath, StringComparison.OrdinalIgnoreCase));

        if (screenCount > 0 && (stylesArtefact is null || appArtefact is null || dataArtefact is null))
        {
            _logger.LogWarning(
                "PrototypeAssembly: screen fragments exist but required fragments missing " +
                "(styles={StylesMissing}, app={AppMissing}, data={DataMissing}), skipping assembly",
                stylesArtefact is null, appArtefact is null, dataArtefact is null);
            return null;
        }

        var styles = await LoadOptionalFragmentContentAsync(stylesArtefact, cancellationToken);
        var appJs = await LoadOptionalFragmentContentAsync(appArtefact, cancellationToken);
        var dataJs = await LoadOptionalFragmentContentAsync(dataArtefact, cancellationToken);

        return (styles, appJs, dataJs);
    }

    private async Task<string> LoadOptionalFragmentContentAsync(Artefact? artefact, CancellationToken cancellationToken)
    {
        if (artefact is null)
        {
            return string.Empty;
        }

        return await _artefactStorageService.GetContentAsync(artefact.S3Key, cancellationToken) ?? string.Empty;
    }

    private async Task<(string ScreensHtml, string NavHtml)> BuildScreensAndNavAsync(
        IReadOnlyList<Artefact> screenFragments,
        CancellationToken cancellationToken)
    {
        var screensBuilder = new System.Text.StringBuilder();
        var navBuilder = new System.Text.StringBuilder();

        foreach (var screenArtefact in screenFragments)
        {
            var screenContent = await _artefactStorageService.GetContentAsync(screenArtefact.S3Key, cancellationToken);
            if (screenContent is not null)
            {
                screensBuilder.Append(screenContent);
            }

            var navLabel = BuildNavLabel(screenArtefact.FilePath);
            var screenId = BuildScreenId(screenArtefact.FilePath);
            navBuilder.AppendLine(System.Globalization.CultureInfo.InvariantCulture,
                $"<li><a href=\"#{screenId}\" onclick=\"showScreen('{screenId}')\">{navLabel}</a></li>");
        }

        return (screensBuilder.ToString(), navBuilder.ToString());
    }

    private static string BuildAssembledDocument(
        string shell,
        (string Styles, string AppJs, string DataJs) fragments,
        string screensHtml,
        string navHtml)
    {
        return shell
            .Replace(MarkerStyles, $"<style>\n{fragments.Styles}\n</style>", StringComparison.Ordinal)
            .Replace(MarkerNav, navHtml, StringComparison.Ordinal)
            .Replace(MarkerScreens, screensHtml, StringComparison.Ordinal)
            .Replace(MarkerData, $"<script>\n{fragments.DataJs}\n</script>", StringComparison.Ordinal)
            .Replace(MarkerApp, $"<script>\n{fragments.AppJs}\n</script>", StringComparison.Ordinal);
    }

    private static string? ValidateAssembledOutput(string html)
    {
        if (!html.Contains("id=\"prototype-metadata\"", StringComparison.OrdinalIgnoreCase))
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

    private async Task PersistAssembledPrototypeAsync(
        Guid projectId,
        string assembled,
        int screenCount,
        CancellationToken cancellationToken)
    {
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
            "system-assembly", _timeProvider, true);

        await _artefactRepository.AddAsync(outputArtefact, cancellationToken);
        await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "PrototypeAssembly: assembled {OutputPath} v{Version} ({Screens} screens, {Bytes} bytes)",
            OutputPath, nextVersion, screenCount,
            System.Text.Encoding.UTF8.GetByteCount(assembled));
    }
}
