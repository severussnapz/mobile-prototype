using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ProjectNoteAggregate;
using Genesis.AI.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Genesis.AI.Infrastructure.Repositories;

public class ProjectNoteRepository : IProjectNoteRepository
{
    private readonly GenesisAiDbContext _context;

    public ProjectNoteRepository(GenesisAiDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IUnitOfWork UnitOfWork => _context;

    public async Task AddAsync(ProjectNote note, CancellationToken cancellationToken)
    {
        await _context.ProjectNotes.AddAsync(note, cancellationToken);
    }

    public async Task<ProjectNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.ProjectNotes
            .FirstOrDefaultAsync(note => note.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectNote>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken)
    {
        return await _context.ProjectNotes
            .AsNoTracking()
            .Where(note => note.ProjectId == projectId)
            .OrderByDescending(note => note.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public void Remove(ProjectNote note)
    {
        _context.ProjectNotes.Remove(note);
    }
}
