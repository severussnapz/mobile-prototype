using MediatR;

namespace Genesis.AI.Domain.Commands.SetPhase;

public record SetPhaseCommand(Guid ConversationId, int Phase) : IRequest<SetPhaseResult>;
