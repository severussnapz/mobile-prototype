using MediatR;

namespace Genesis.AI.Domain.Commands.SendMessage;

public record SendMessageCommand(
    Guid ConversationId,
    string Content,
    string UserId,
    string? UserErn = null,
    string? GivenName = null,
    string? FamilyName = null) : IRequest<Guid>;
