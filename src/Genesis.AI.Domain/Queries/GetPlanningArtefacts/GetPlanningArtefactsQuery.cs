using MediatR;

namespace Genesis.AI.Domain.Queries.GetPlanningArtefacts;

public sealed record GetPlanningArtefactsQuery(Guid ProjectId)
    : IRequest<GetPlanningArtefactsResult>;
