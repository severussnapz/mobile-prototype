using MediatR;

namespace Genesis.AI.Domain.Queries.GetPlanningStatus;

public sealed record GetPlanningStatusQuery(Guid ProjectId)
    : IRequest<GetPlanningStatusResult>;
