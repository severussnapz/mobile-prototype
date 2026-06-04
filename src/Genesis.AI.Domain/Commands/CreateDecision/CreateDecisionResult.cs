using Genesis.AI.Domain.AggregatesModel.ProjectDecisionAggregate;

namespace Genesis.AI.Domain.Commands.CreateDecision;

public record CreateDecisionResult(bool ProjectFound, ProjectDecision? Decision = null);
