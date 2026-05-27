using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using MediatR;

namespace Genesis.AI.Domain.Queries.GetArtefactsByStage;

public record GetArtefactsByStageQuery(Guid ProjectId) : IRequest<IReadOnlyList<Artefact>>;
