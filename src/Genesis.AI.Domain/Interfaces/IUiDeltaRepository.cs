using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.UiDeltaAggregate;

namespace Genesis.AI.Domain.Interfaces;

public interface IUiDeltaRepository
{
    IUnitOfWork UnitOfWork { get; }

    Task AddAsync(UiDelta uiDelta, CancellationToken cancellationToken);

    Task<IReadOnlyList<UiDelta>> GetUnlockedSubstantiveByRequirementAsync(
        Guid projectId,
        string requirementId,
        CancellationToken cancellationToken);

    Task MarkLockedBatchAsync(
        IReadOnlyList<Guid> ids,
        Guid lockBatchId,
        string requirementFilePath,
        DateTimeOffset lockedAt,
        CancellationToken cancellationToken);
}
