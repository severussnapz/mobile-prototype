using MediatR;

namespace Genesis.AI.Domain.Queries.GetNormalisationStatus;

public sealed record GetNormalisationStatusQuery(Guid ProjectId)
    : IRequest<GetNormalisationStatusResult>;
