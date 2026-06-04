using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.DeleteNote;

public class DeleteNoteCommandHandler : IRequestHandler<DeleteNoteCommand, bool>
{
    private readonly IProjectNoteRepository _noteRepository;

    public DeleteNoteCommandHandler(IProjectNoteRepository noteRepository)
    {
        _noteRepository = noteRepository ?? throw new ArgumentNullException(nameof(noteRepository));
    }

    public async Task<bool> Handle(DeleteNoteCommand request, CancellationToken cancellationToken)
    {
        var note = await _noteRepository.GetByIdAsync(request.NoteId, cancellationToken);
        if (note is null || note.ProjectId != request.ProjectId)
            return false;

        _noteRepository.Remove(note);
        await _noteRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
