using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ProjectNoteAggregate;

namespace Genesis.AI.Domain.Interfaces;

public interface IProjectNoteRepository
{
    IUnitOfWork UnitOfWork { get; }

    Task AddAsync(ProjectNote note, CancellationToken cancellationToken);
    Task<ProjectNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProjectNote>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken);
    void Remove(ProjectNote note);
}
