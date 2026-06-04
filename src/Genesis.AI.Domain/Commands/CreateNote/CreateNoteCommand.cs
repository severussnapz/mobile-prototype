using MediatR;

namespace Genesis.AI.Domain.Commands.CreateNote;

public record CreateNoteCommand(
    Guid ProjectId,
    string Content,
    string? AuthorErn,
    string? AuthorGivenName,
    string? AuthorFamilyName) : IRequest<CreateNoteResult>;
