using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;
using Microsoft.EntityFrameworkCore;

namespace Genesis.AI.Infrastructure.Repositories;

public sealed class RequirementChangeRepository : IRequirementChangeRepository
{
    private readonly GenesisAiDbContext _context;

    public RequirementChangeRepository(GenesisAiDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IUnitOfWork UnitOfWork => _context;

    public async Task AddAsync(
        RequirementChange change,
        CancellationToken cancellationToken)
    {
        await _context.RequirementChanges.AddAsync(change, cancellationToken);
    }

    public async Task<RequirementChange?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await _context.RequirementChanges
            .FirstOrDefaultAsync(change => change.Id == id, cancellationToken);
    }

    public async Task<RequirementChange?> GetByIdForProjectAsync(
        Guid id,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        return await _context.RequirementChanges
            .FirstOrDefaultAsync(
                change => change.Id == id && change.ProjectId == projectId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<RequirementChange>> GetByProjectIdAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        return await _context.RequirementChanges
            .Where(change => change.ProjectId == projectId)
            .OrderByDescending(change => change.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RequirementChange>> GetPendingByProjectIdAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        return await _context.RequirementChanges
            .Where(change => change.ProjectId == projectId &&
                             change.Status == ChangeStatus.Pending)
            .OrderBy(change => change.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasOpenDefiniteReviewsAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        return await _context.RequirementChanges
            .Where(change => change.ProjectId == projectId &&
                             change.Status == ChangeStatus.Approved)
            .AnyAsync(change =>
                (change.ClinicalSafetyImpact == ImpactLevel.Definite &&
                 !change.ClinicalSafetyReviewed) ||
                (change.IgImpact == ImpactLevel.Definite &&
                 !change.IgReviewed) ||
                (change.SecurityImpact == ImpactLevel.Definite &&
                 !change.SecurityReviewed),
                cancellationToken);
    }
}
