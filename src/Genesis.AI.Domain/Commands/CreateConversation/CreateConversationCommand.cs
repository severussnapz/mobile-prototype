using MediatR;

namespace Genesis.AI.Domain.Commands.CreateConversation;

public record CreateConversationCommand(Guid StageId) : IRequest<Guid>;
