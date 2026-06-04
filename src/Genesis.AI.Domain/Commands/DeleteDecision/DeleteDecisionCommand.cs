using MediatR;

namespace Genesis.AI.Domain.Commands.DeleteDecision;

public record DeleteDecisionCommand(Guid ProjectId, Guid DecisionId) : IRequest<bool>;
