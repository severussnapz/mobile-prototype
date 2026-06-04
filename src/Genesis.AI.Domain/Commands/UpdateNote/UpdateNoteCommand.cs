using MediatR;

namespace Genesis.AI.Domain.Commands.UpdateNote;

public record UpdateNoteCommand(Guid ProjectId, Guid NoteId, string Content) : IRequest<UpdateNoteResult>;
