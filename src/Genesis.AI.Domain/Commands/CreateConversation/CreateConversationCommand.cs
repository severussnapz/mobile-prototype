using MediatR;

namespace Genesis.AI.Domain.Commands.CreateConversation;

public record CreateConversationCommand(Guid StageId, string? RequirementId = null) : IRequest<Guid>;
