using MediatR;

namespace Genesis.AI.Domain.Commands.AdvancePhase;

public record AdvancePhaseCommand(Guid ConversationId) : IRequest<AdvancePhaseResult>;
