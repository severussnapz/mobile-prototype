using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;

namespace Genesis.AI.Domain.Interfaces;

public interface IProjectRepository
{
    IUnitOfWork UnitOfWork { get; }

    Task AddAsync(Project project, CancellationToken cancellationToken);
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Project>> GetByStatusAsync(string status, CancellationToken cancellationToken);
    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the project that owns the given pipeline stage, including all stages.
    /// </summary>
    Task<Project?> GetByStageIdAsync(Guid stageId, CancellationToken cancellationToken);
}
