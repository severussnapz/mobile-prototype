using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.PrototypeLockAggregate;
using Genesis.AI.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Genesis.AI.Infrastructure.Repositories;

public sealed class PrototypeLockRepository : IPrototypeLockRepository
{
    private readonly GenesisAiDbContext _context;
    private readonly TimeProvider _timeProvider;

    public PrototypeLockRepository(GenesisAiDbContext context, TimeProvider timeProvider)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public IUnitOfWork UnitOfWork => _context;

    public async Task<PrototypeLock?> GetByStageIdAsync(Guid stageId, CancellationToken cancellationToken)
    {
        return await _context.Set<PrototypeLock>()
            .FirstOrDefaultAsync(lockRow => lockRow.StageId == stageId, cancellationToken);
    }

    public async Task<PrototypeLock?> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken)
    {
        return await _context.Set<PrototypeLock>()
            .FirstOrDefaultAsync(lockRow => lockRow.ProjectId == projectId, cancellationToken);
    }

    public async Task AddAsync(PrototypeLock prototypeLock, CancellationToken cancellationToken)
    {
        await _context.Set<PrototypeLock>().AddAsync(prototypeLock, cancellationToken);
    }

    public async Task ClearByStageIdAsync(Guid stageId, CancellationToken cancellationToken)
    {
        var existing = await _context.Set<PrototypeLock>()
            .FirstOrDefaultAsync(lockRow => lockRow.StageId == stageId, cancellationToken);

        if (existing is null)
        {
            return;
        }

        existing.ClearLock(_timeProvider);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
