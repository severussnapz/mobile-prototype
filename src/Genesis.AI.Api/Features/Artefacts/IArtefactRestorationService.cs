using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using JsonApi.Resources.Queries;

namespace Genesis.AI.Api.Features.Artefacts;

/// <summary>
/// Handles artefact version restoration including DB-tracked versions
/// and S3 fallback for historical prototype versions.
/// </summary>
public interface IArtefactRestorationService
{
    /// <summary>
    /// Restores an artefact to a specific version from DB or S3 fallback.
    /// </summary>
    Task<Artefact?> RestoreArtefactVersionAsync(
        Guid projectId,
        string filePath,
        int version,
        string? userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Builds S3 fallback version list for files without DB entries (prototype only).
    /// </summary>
    Task<List<ArtefactVersionResponse>> BuildS3FallbackVersionsAsync(
        Guid projectId,
        string filePath,
        PaginationFilter pagination,
        CancellationToken cancellationToken);
}
