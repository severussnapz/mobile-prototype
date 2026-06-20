using System.Text;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Genesis.AI.Infrastructure.Services;

/// <summary>
/// Builds the Category A (stable foundation) content for a given stage by fetching
/// upstream artefacts from storage and assembling them into a single prompt section.
/// The output is placed before the Bedrock cache point so it is cached across turns
/// (approximately 10× cheaper for the cached tokens).
///
/// Safe logging: only artefact counts and character lengths are logged — never content
/// (SEC-003 — no sensitive artefact content in logs).
/// </summary>
public sealed class FoundationService : IFoundationService
{
    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;
    private readonly ILogger<FoundationService> _logger;

    public FoundationService(
        IArtefactRepository artefactRepository,
        IArtefactStorageService artefactStorageService,
        ILogger<FoundationService> logger)
    {
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _artefactStorageService = artefactStorageService ?? throw new ArgumentNullException(nameof(artefactStorageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<string> BuildFoundationContentAsync(
        Guid projectId,
        StageType stageType,
        CancellationToken cancellationToken)
    {
        var prefixes = StageFoundationMap.GetFoundationPrefixes(stageType);

        if (prefixes.Count == 0)
        {
            _logger.LogDebug(
                "No foundation prefixes for stage {StageType} — foundation caching not applicable",
                stageType);
            return string.Empty;
        }

        // Retrieve the full artefact manifest (lightweight — file paths, versions, S3 keys)
        var allArtefacts = await _artefactRepository.GetProjectArtefactManifestAsync(
            projectId, cancellationToken);

        // Filter to Category A artefacts matching this stage's foundation prefixes
        var foundationArtefacts = allArtefacts
            .Where(artefact => StageFoundationMap.IsFoundationArtefact(stageType, artefact.FilePath))
            .OrderBy(artefact => artefact.FilePath)
            .ToList();

        if (foundationArtefacts.Count == 0)
        {
            _logger.LogDebug(
                "No foundation artefacts found for project {ProjectId}, stage {StageType}",
                projectId, stageType);
            return string.Empty;
        }

        _logger.LogInformation(
            "Building foundation content for project {ProjectId}, stage {StageType}: " +
            "{ArtefactCount} artefact(s) matched",
            projectId, stageType, foundationArtefacts.Count);

        var builder = new StringBuilder();
        AppendFoundationHeader(builder);

        var (loadedCount, totalChars) = await AppendFoundationArtefactsAsync(
            builder, foundationArtefacts, cancellationToken);

        _logger.LogInformation(
            "Foundation content built for project {ProjectId}, stage {StageType}: " +
            "{LoadedCount}/{ArtefactCount} artefact(s), {TotalChars} total chars",
            projectId, stageType, loadedCount, foundationArtefacts.Count, totalChars);

        return builder.ToString();
    }

    private static void AppendFoundationHeader(StringBuilder builder)
    {
        builder.AppendLine("## PROJECT FOUNDATION");
        builder.AppendLine();
        builder.AppendLine("The following upstream artefacts are loaded in full in this system context.");
        builder.AppendLine("**Do not call `get_artefact` for any file listed here** — the content is already available below.");
        builder.AppendLine("Use `get_artefact` only for files not listed in PROJECT FOUNDATION or for live tracking artefacts.");
        builder.AppendLine();
    }

    private async Task<(int LoadedCount, int TotalChars)> AppendFoundationArtefactsAsync(
        StringBuilder builder,
        IReadOnlyList<Artefact> foundationArtefacts,
        CancellationToken cancellationToken)
    {
        var totalChars = 0;
        var loadedCount = 0;

        foreach (var artefact in foundationArtefacts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var content = await LoadFoundationArtefactContentAsync(artefact, cancellationToken);
            if (content is null)
            {
                continue;
            }

            AppendFoundationArtefactSection(builder, artefact, content, out var appendedChars);
            totalChars += appendedChars;

            loadedCount++;
        }

        return (loadedCount, totalChars);
    }

    private async Task<string?> LoadFoundationArtefactContentAsync(
        Artefact artefact,
        CancellationToken cancellationToken)
    {
        var content = await _artefactStorageService.GetContentAsync(artefact.S3Key, cancellationToken);
        if (content is null)
        {
            _logger.LogWarning(
                "Foundation artefact {FilePath} (v{Version}) not found in storage — skipping",
                artefact.FilePath,
                artefact.Version);
            return null;
        }

        // SEC-003: log only path and character length — never content
        _logger.LogDebug(
            "Loaded foundation artefact {FilePath} (v{Version}): {CharCount} chars",
            artefact.FilePath,
            artefact.Version,
            content.Length);

        return content;
    }

    private static void AppendFoundationArtefactSection(
        StringBuilder builder,
        Artefact artefact,
        string content,
        out int appendedChars)
    {
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"### {artefact.FilePath} (v{artefact.Version})");
        builder.AppendLine();

        var sectionContent = BuildCachedSectionContent(content, artefact.FilePath, out appendedChars);
        builder.AppendLine(sectionContent);
        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();
    }

    private static string BuildCachedSectionContent(string content, string filePath, out int appendedChars)
    {
        const int largeFileThreshold = 50_000; // 50KB threshold — use outline for larger files
        if (content.Length <= largeFileThreshold)
        {
            appendedChars = content.Length;
            return content;
        }

        var outline = BuildFileOutline(content, filePath);
        appendedChars = outline.Length;
        return "**OUTLINE** (file too large for full caching — use `get_artefact` for full content):\n\n" + outline;
    }

    /// <summary>
    /// Builds a structural outline for large files (50KB+) to reduce cache write cost.
    /// Extracts CSS variables, selectors, HTML elements, and section comments.
    /// </summary>
    private static string BuildFileOutline(string content, string filePath)
    {
        var sb = new System.Text.StringBuilder();
        var ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
        if (ext is not ".html" and not ".htm" and not ".css")
        {
            return BuildNonHtmlOutline(content);
        }

        AppendCssCustomProperties(sb, content);
        AppendCssSelectors(sb, content);
        AppendHtmlElements(sb, content);

        return sb.ToString();
    }

    private static string BuildNonHtmlOutline(string content)
    {
        var sb = new StringBuilder();
        sb.AppendLine("(First 1000 chars only)");
        sb.AppendLine(content.Length > 1000 ? content[..1000] + "\n[...truncated...]" : content);
        return sb.ToString();
    }

    private static void AppendCssCustomProperties(StringBuilder sb, string content)
    {
        sb.AppendLine("#### CSS Custom Properties");
        var rootMatch = System.Text.RegularExpressions.Regex.Match(
            content,
            @":root\s*\{([^}]+)\}",
            System.Text.RegularExpressions.RegexOptions.Singleline);

        if (!rootMatch.Success)
        {
            sb.AppendLine("(none)");
            sb.AppendLine();
            return;
        }

        var properties = System.Text.RegularExpressions.Regex.Matches(rootMatch.Groups[1].Value, @"--[\w-]+");
        foreach (System.Text.RegularExpressions.Match property in properties)
        {
            sb.Append("- ").AppendLine(property.Value);
        }

        sb.AppendLine();
    }

    private static void AppendCssSelectors(StringBuilder sb, string content)
    {
        sb.AppendLine("#### CSS Selectors");
        var selectorMatches = System.Text.RegularExpressions.Regex.Matches(
            content,
            @"(?:^|\})\s*((?:[.#][\w-]+(?:\s+[.#>+~]?[\w-]+)*(?::[:\w-]+)?(?:\s*,\s*)?)+)\s*\{",
            System.Text.RegularExpressions.RegexOptions.Multiline);

        var seenSelectors = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        foreach (System.Text.RegularExpressions.Match selectorMatch in selectorMatches)
        {
            var selector = selectorMatch.Groups[1].Value.Trim();
            if (selector.Length > 0 && seenSelectors.Add(selector))
            {
                sb.Append("- ").AppendLine(selector);
            }
        }

        sb.AppendLine();
    }

    private static void AppendHtmlElements(StringBuilder sb, string content)
    {
        sb.AppendLine("#### HTML Elements (id/class)");
        var elementMatches = System.Text.RegularExpressions.Regex.Matches(
            content,
            @"<(?:div|nav|main|section|article|aside|header|footer)\s+(?:id=[""']([^""']+)[""']|class=[""']([^""']+)[""'])+",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        var foundElements = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        foreach (System.Text.RegularExpressions.Match elementMatch in elementMatches)
        {
            var id = elementMatch.Groups[1].Value;
            var className = elementMatch.Groups[2].Value;
            var key = string.IsNullOrEmpty(id) ? $".{className}" : $"#{id}";
            if (foundElements.Add(key))
            {
                sb.Append("- ").AppendLine(key);
            }
        }
    }
}
