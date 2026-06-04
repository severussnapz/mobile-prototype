using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.UpdateNote;

public class UpdateNoteCommandHandler : IRequestHandler<UpdateNoteCommand, UpdateNoteResult>
{
    private readonly IProjectNoteRepository _noteRepository;
    private readonly TimeProvider _timeProvider;

    public UpdateNoteCommandHandler(
        IProjectNoteRepository noteRepository,
        TimeProvider timeProvider)
    {
        _noteRepository = noteRepository ?? throw new ArgumentNullException(nameof(noteRepository));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<UpdateNoteResult> Handle(UpdateNoteCommand request, CancellationToken cancellationToken)
    {
        var note = await _noteRepository.GetByIdAsync(request.NoteId, cancellationToken);
        if (note is null || note.ProjectId != request.ProjectId)
            return new UpdateNoteResult(Found: false);

        note.UpdateContent(request.Content, _timeProvider);
        await _noteRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateNoteResult(Found: true, Note: note);
    }
}
