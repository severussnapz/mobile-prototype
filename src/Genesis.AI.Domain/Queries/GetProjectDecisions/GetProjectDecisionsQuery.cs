using Genesis.AI.Domain.AggregatesModel.ProjectDecisionAggregate;
using MediatR;

namespace Genesis.AI.Domain.Queries.GetProjectDecisions;

public record GetProjectDecisionsQuery(Guid ProjectId) : IRequest<IReadOnlyList<ProjectDecision>?>;
