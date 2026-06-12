using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;

namespace Genesis.AI.Domain.Interfaces;

public interface IArtefactRepository
{
    IUnitOfWork UnitOfWork { get; }

    Task AddAsync(Artefact artefact, CancellationToken cancellationToken);
    Task<Artefact?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Artefact>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken);
    Task<int> GetNextVersionAsync(Guid projectId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the next version number for a specific file path within a project.
    /// Used when the AI progressively updates a file (DRAFT → final).
    /// </summary>
    Task<int> GetNextVersionForFileAsync(Guid projectId, string filePath, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes all artefact versions for a given project/file older than the specified version.
    /// Called after saving a new version to keep only the latest.
    /// </summary>
    Task DeletePreviousVersionsAsync(Guid projectId, string filePath, int currentVersion, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a lightweight manifest of all artefacts for a project (paths, versions, timestamps).
    /// Used to inject a small listing into the system prompt.
    /// </summary>
    Task<IReadOnlyList<Artefact>> GetProjectArtefactManifestAsync(Guid projectId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a single artefact by project ID and file path (latest version).
    /// Used by the get_artefact tool.
    /// </summary>
    Task<Artefact?> GetByProjectAndFilePathAsync(Guid projectId, string filePath, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the timestamp of the most recently created/modified artefact for a project.
    /// Used for staleness detection.
    /// </summary>
    Task<DateTimeOffset?> GetLatestArtefactTimestampAsync(Guid projectId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns true if the project already has at least one saved requirements artefact
    /// (any file matching requirements/REQ-*.md). Used by the advance_requirement completion gate.
    /// </summary>
    Task<bool> HasRequirementArtefactAsync(Guid projectId, CancellationToken cancellationToken);
}
