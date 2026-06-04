using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ProjectDecisionAggregate;
using Genesis.AI.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Genesis.AI.Infrastructure.Repositories;

public class ProjectDecisionRepository : IProjectDecisionRepository
{
    private readonly GenesisAiDbContext _context;

    public ProjectDecisionRepository(GenesisAiDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IUnitOfWork UnitOfWork => _context;

    public async Task AddAsync(ProjectDecision decision, CancellationToken cancellationToken)
    {
        await _context.ProjectDecisions.AddAsync(decision, cancellationToken);
    }

    public async Task<ProjectDecision?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.ProjectDecisions
            .FirstOrDefaultAsync(decision => decision.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectDecision>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken)
    {
        return await _context.ProjectDecisions
            .AsNoTracking()
            .Where(decision => decision.ProjectId == projectId)
            .OrderByDescending(decision => decision.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public void Remove(ProjectDecision decision)
    {
        _context.ProjectDecisions.Remove(decision);
    }
}
