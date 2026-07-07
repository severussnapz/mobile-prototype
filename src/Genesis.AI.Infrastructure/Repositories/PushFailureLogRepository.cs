using Genesis.AI.Domain.AggregatesModel.PushFailureLogAggregate;
using Genesis.AI.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Genesis.AI.Infrastructure.Repositories;

public sealed class PushFailureLogRepository : IPushFailureLogRepository
{
    private readonly GenesisAiDbContext _context;

    public PushFailureLogRepository(GenesisAiDbContext context)
        => _context = context;

    public async Task AddAsync(PushFailureLog log, CancellationToken ct)
    {
        await _context.PushFailureLogs.AddAsync(log, ct);
        await _context.SaveChangesAsync(ct);
    }

    public Task<int> GetUnresolvedCountAsync(Guid projectId, CancellationToken ct)
        => _context.PushFailureLogs
            .CountAsync(p => p.ProjectId == projectId && p.ResolvedAt == null, ct);
}
