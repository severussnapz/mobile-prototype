using Genesis.AI.Domain.AggregatesModel.ProjectNoteAggregate;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.CreateNote;

public class CreateNoteCommandHandler : IRequestHandler<CreateNoteCommand, CreateNoteResult>
{
    private readonly IProjectNoteRepository _noteRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly TimeProvider _timeProvider;

    public CreateNoteCommandHandler(
        IProjectNoteRepository noteRepository,
        IProjectRepository projectRepository,
        TimeProvider timeProvider)
    {
        _noteRepository = noteRepository ?? throw new ArgumentNullException(nameof(noteRepository));
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<CreateNoteResult> Handle(CreateNoteCommand request, CancellationToken cancellationToken)
    {
        var projectExists = await _projectRepository.ExistsAsync(request.ProjectId, cancellationToken);
        if (!projectExists)
            return new CreateNoteResult(ProjectFound: false);

        var note = new ProjectNote(
            request.ProjectId,
            request.Content,
            request.AuthorErn,
            request.AuthorGivenName,
            request.AuthorFamilyName,
            _timeProvider);

        await _noteRepository.AddAsync(note, cancellationToken);
        await _noteRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateNoteResult(ProjectFound: true, Note: note);
    }
}
