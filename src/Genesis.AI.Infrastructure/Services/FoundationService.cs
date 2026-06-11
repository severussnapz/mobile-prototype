using System.Text;
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
        builder.AppendLine("## PROJECT FOUNDATION");
        builder.AppendLine();
        builder.AppendLine("The following upstream artefacts are loaded in full in this system context.");
        builder.AppendLine("**Do not call `get_artefact` for any file listed here** — the content is already available below.");
        builder.AppendLine("Use `get_artefact` only for files not listed in PROJECT FOUNDATION or for live tracking artefacts.");
        builder.AppendLine();

        var totalChars = 0;
        var loadedCount = 0;

        foreach (var artefact in foundationArtefacts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var content = await _artefactStorageService.GetContentAsync(
                artefact.S3Key, cancellationToken);

            if (content is null)
            {
                _logger.LogWarning(
                    "Foundation artefact {FilePath} (v{Version}) not found in storage — skipping",
                    artefact.FilePath, artefact.Version);
                continue;
            }

            // SEC-003: log only path and character length — never content
            _logger.LogDebug(
                "Loaded foundation artefact {FilePath} (v{Version}): {CharCount} chars",
                artefact.FilePath, artefact.Version, content.Length);

            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"### {artefact.FilePath} (v{artefact.Version})");
            builder.AppendLine();
            builder.AppendLine(content);
            builder.AppendLine();
            builder.AppendLine("---");
            builder.AppendLine();

            totalChars += content.Length;
            loadedCount++;
        }

        _logger.LogInformation(
            "Foundation content built for project {ProjectId}, stage {StageType}: " +
            "{LoadedCount}/{ArtefactCount} artefact(s), {TotalChars} total chars",
            projectId, stageType, loadedCount, foundationArtefacts.Count, totalChars);

        return builder.ToString();
    }
}
