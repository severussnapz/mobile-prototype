using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.UiDeltaAggregate;
using Genesis.AI.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Genesis.AI.Infrastructure.Repositories;

public sealed class UiDeltaRepository : IUiDeltaRepository
{
    private readonly GenesisAiDbContext _context;

    public UiDeltaRepository(GenesisAiDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IUnitOfWork UnitOfWork => _context;

    public async Task AddAsync(UiDelta uiDelta, CancellationToken cancellationToken)
    {
        await _context.Set<UiDelta>().AddAsync(uiDelta, cancellationToken);
    }

    public async Task<IReadOnlyList<UiDelta>> GetUnlockedSubstantiveByRequirementAsync(
        Guid projectId,
        string requirementId,
        CancellationToken cancellationToken)
    {
        return await _context.Set<UiDelta>()
            .Where(delta => delta.ProjectId == projectId
                && delta.RequirementId == requirementId
                && delta.LockedAt == null
                && delta.RequirementImpact == Domain.Enums.RequirementImpact.Substantive)
            .OrderBy(delta => delta.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkLockedBatchAsync(
        IReadOnlyList<Guid> ids,
        Guid lockBatchId,
        string requirementFilePath,
        DateTimeOffset lockedAt,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return;
        }

        var deltas = await _context.Set<UiDelta>()
            .Where(delta => ids.Contains(delta.Id))
            .ToListAsync(cancellationToken);

        foreach (var delta in deltas)
        {
            delta.MarkLocked(lockBatchId, requirementFilePath, lockedAt);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
