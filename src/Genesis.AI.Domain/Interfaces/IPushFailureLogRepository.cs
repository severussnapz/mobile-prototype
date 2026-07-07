using Genesis.AI.Domain.AggregatesModel.PushFailureLogAggregate;

namespace Genesis.AI.Domain.Interfaces;

public interface IPushFailureLogRepository
{
    Task AddAsync(PushFailureLog log, CancellationToken ct);

    Task<int> GetUnresolvedCountAsync(Guid projectId, CancellationToken ct);
}
