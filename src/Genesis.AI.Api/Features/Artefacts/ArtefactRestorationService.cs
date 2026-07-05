using System.Text;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;
using JsonApi.Resources.Queries;

namespace Genesis.AI.Api.Features.Artefacts;

/// <summary>
/// Handles artefact version restoration logic including DB-tracked versions
/// and S3 fallback for historical prototype versions no longer in the DB.
/// </summary>
internal sealed class ArtefactRestorationService : IArtefactRestorationService
{
    private const string PrototypeHtmlArtefactPath = "prototype/index.html";
    private const string PrototypeHtmlContentType = "text/html";

    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;
    private readonly TimeProvider _timeProvider;

    public ArtefactRestorationService(
        IArtefactRepository artefactRepository,
        IArtefactStorageService artefactStorageService,
        TimeProvider timeProvider)
    {
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _artefactStorageService = artefactStorageService ?? throw new ArgumentNullException(nameof(artefactStorageService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<Artefact?> RestoreArtefactVersionAsync(
        Guid projectId,
        string filePath,
        int version,
        string? userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        var normalisedFilePath = filePath.Trim();
        var versions = await _artefactRepository.GetVersionsByFilePathAsync(projectId, normalisedFilePath, cancellationToken);

        var sourceVersion = versions.FirstOrDefault(artefact => artefact.Version == version);
        if (sourceVersion is null)
        {
            // Try S3 fallback for prototype versions
            return await RestorePrototypeVersionFromS3Async(projectId, normalisedFilePath, version, userId, cancellationToken);
        }

        var latestVersion = versions.Max(artefact => artefact.Version);
        if (version == latestVersion)
        {
            // Already the latest version
            return sourceVersion;
        }

        // Restore from S3
        var sourceContent = await _artefactStorageService.GetContentAsync(sourceVersion.S3Key, cancellationToken);
        if (string.IsNullOrEmpty(sourceContent))
            return null;

        var nextVersion = await _artefactRepository.GetNextVersionForFileAsync(projectId, normalisedFilePath, cancellationToken);
        var restoredContentType = sourceVersion.ContentType;

        var newStorageKey = await _artefactStorageService.SaveContentAsync(
            projectId,
            normalisedFilePath,
            nextVersion,
            sourceContent,
            restoredContentType,
            cancellationToken);

        var userId_actual = userId ?? "system";
        var restoredArtefact = Artefact.CreateS3Artefact(
            projectId,
            nextVersion,
            normalisedFilePath,
            newStorageKey,
            restoredContentType,
            Encoding.UTF8.GetByteCount(sourceContent),
            userId_actual,
            _timeProvider,
            true);

        await _artefactRepository.AddAsync(restoredArtefact, cancellationToken);
        await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return restoredArtefact;
    }

    private async Task<Artefact?> RestorePrototypeVersionFromS3Async(
        Guid projectId,
        string normalisedFilePath,
        int version,
        string? userId,
        CancellationToken cancellationToken)
    {
        // S3 is the source of truth for prototype history when the database no longer tracks the version.
        if (!string.Equals(normalisedFilePath, PrototypeHtmlArtefactPath, StringComparison.OrdinalIgnoreCase))
            return null;

        var s3Key = $"projects/{projectId}/artefacts/{PrototypeHtmlArtefactPath}/v{version}";
        var sourceContent = await _artefactStorageService.GetContentAsync(s3Key, cancellationToken);
        if (string.IsNullOrEmpty(sourceContent))
            return null;

        var nextVersion = await _artefactRepository.GetNextVersionForFileAsync(projectId, normalisedFilePath, cancellationToken);

        var newStorageKey = await _artefactStorageService.SaveContentAsync(
            projectId,
            normalisedFilePath,
            nextVersion,
            sourceContent,
            PrototypeHtmlContentType,
            cancellationToken);

        var actualUserId = userId ?? "system";
        var restoredArtefact = Artefact.CreateS3Artefact(
            projectId,
            nextVersion,
            normalisedFilePath,
            newStorageKey,
            PrototypeHtmlContentType,
            Encoding.UTF8.GetByteCount(sourceContent),
            actualUserId,
            _timeProvider,
            true);

        await _artefactRepository.AddAsync(restoredArtefact, cancellationToken);
        await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return restoredArtefact;
    }

    public async Task<List<ArtefactVersionResponse>> BuildS3FallbackVersionsAsync(
        Guid projectId,
        string filePath,
        PaginationFilter pagination,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(filePath.Trim(), PrototypeHtmlArtefactPath, StringComparison.OrdinalIgnoreCase))
            return [];

        var s3Versions = await _artefactStorageService.ListVersionsAsync(projectId, PrototypeHtmlArtefactPath, cancellationToken);
        if (s3Versions.Count == 0)
            return [];

        var page = Math.Max(1, pagination.Page);
        var size = Math.Max(1, pagination.Size);
        var skip = (page - 1) * size;

        return s3Versions
            .Skip(skip)
            .Take(size)
            .Select(entry => new ArtefactVersionResponse
            {
                Id = Guid.Empty,
                Version = entry.Version,
                CreatedAt = entry.LastModified,
                CreatedBy = "system",
                SizeBytes = entry.SizeBytes,
                ContentType = PrototypeHtmlContentType
            })
            .ToList();
    }
}
