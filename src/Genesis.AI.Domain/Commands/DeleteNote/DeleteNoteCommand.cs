using MediatR;

namespace Genesis.AI.Domain.Commands.DeleteNote;

public record DeleteNoteCommand(Guid ProjectId, Guid NoteId) : IRequest<bool>;
