using MediatR;

namespace Genesis.AI.Domain.Queries.GetNormalisationArtefacts;

public sealed record GetNormalisationArtefactsQuery(Guid ProjectId)
    : IRequest<GetNormalisationArtefactsResult>;
