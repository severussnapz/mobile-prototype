using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.PrototypeLockAggregate;

namespace Genesis.AI.Domain.Interfaces;

public interface IPrototypeLockRepository
{
    IUnitOfWork UnitOfWork { get; }

    Task<PrototypeLock?> GetByStageIdAsync(Guid stageId, CancellationToken cancellationToken);
    Task<PrototypeLock?> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken);
    Task AddAsync(PrototypeLock prototypeLock, CancellationToken cancellationToken);
    Task ClearByStageIdAsync(Guid stageId, CancellationToken cancellationToken);
}
