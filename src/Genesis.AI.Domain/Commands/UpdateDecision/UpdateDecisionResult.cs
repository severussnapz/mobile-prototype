using Genesis.AI.Domain.AggregatesModel.ProjectDecisionAggregate;

namespace Genesis.AI.Domain.Commands.UpdateDecision;

public record UpdateDecisionResult(bool Found, ProjectDecision? Decision = null);
