using Genesis.AI.Domain.AggregatesModel.ProjectNoteAggregate;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Queries.GetProjectNotes;

public class GetProjectNotesQueryHandler : IRequestHandler<GetProjectNotesQuery, IReadOnlyList<ProjectNote>?>
{
    private readonly IProjectNoteRepository _noteRepository;
    private readonly IProjectRepository _projectRepository;

    public GetProjectNotesQueryHandler(
        IProjectNoteRepository noteRepository,
        IProjectRepository projectRepository)
    {
        _noteRepository = noteRepository ?? throw new ArgumentNullException(nameof(noteRepository));
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
    }

    public async Task<IReadOnlyList<ProjectNote>?> Handle(GetProjectNotesQuery request, CancellationToken cancellationToken)
    {
        var projectExists = await _projectRepository.ExistsAsync(request.ProjectId, cancellationToken);
        if (!projectExists)
            return null;

        return await _noteRepository.GetByProjectIdAsync(request.ProjectId, cancellationToken);
    }
}
