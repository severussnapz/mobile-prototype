using MediatR;

namespace Genesis.AI.Domain.Queries.GetPushStatus;

public sealed record GetPushStatusQuery(Guid ProjectId) : IRequest<GetPushStatusResult>;