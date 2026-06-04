using MediatR;

namespace Genesis.AI.Domain.Commands.UpdateDecision;

public record UpdateDecisionCommand(
    Guid ProjectId,
    Guid DecisionId,
    string Title,
    string Context,
    string Decision,
    string Consequences) : IRequest<UpdateDecisionResult>;
