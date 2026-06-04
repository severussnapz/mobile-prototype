using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Genesis.AI.Infrastructure.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly GenesisAiDbContext _context;

    public ProjectRepository(GenesisAiDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IUnitOfWork UnitOfWork => _context;

    public async Task AddAsync(Project project, CancellationToken cancellationToken)
    {
        await _context.Projects.AddAsync(project, cancellationToken);
    }

    public async Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Projects
            .Include(project => project.PipelineStages)
            .FirstOrDefaultAsync(project => project.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Projects
            .Include(project => project.PipelineStages)
            .AsNoTracking()
            .OrderByDescending(project => project.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> GetByStatusAsync(string status, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ProjectStatus>(status, ignoreCase: true, out var projectStatus))
        {
            return [];
        }

        return await _context.Projects
            .Include(project => project.PipelineStages)
            .AsNoTracking()
            .Where(project => project.Status == projectStatus)
            .OrderByDescending(project => project.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken)
    {
        return await _context.Projects
            .AnyAsync(project => project.Code == code, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Projects
            .AnyAsync(project => project.Id == id && !project.IsDeleted, cancellationToken);
    }

    public async Task<Project?> GetByStageIdAsync(Guid stageId, CancellationToken cancellationToken)
    {
        return await _context.Projects
            .Include(project => project.PipelineStages)
            .FirstOrDefaultAsync(
                project => project.PipelineStages.Any(stage => stage.Id == stageId),
                cancellationToken);
    }
}
