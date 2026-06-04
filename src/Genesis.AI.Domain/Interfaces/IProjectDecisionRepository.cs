using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ProjectDecisionAggregate;

namespace Genesis.AI.Domain.Interfaces;

public interface IProjectDecisionRepository
{
    IUnitOfWork UnitOfWork { get; }

    Task AddAsync(ProjectDecision decision, CancellationToken cancellationToken);
    Task<ProjectDecision?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProjectDecision>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken);
    void Remove(ProjectDecision decision);
}
