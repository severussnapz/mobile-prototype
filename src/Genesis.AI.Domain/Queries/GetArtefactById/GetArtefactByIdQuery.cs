using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using MediatR;

namespace Genesis.AI.Domain.Queries.GetArtefactById;

public record GetArtefactByIdQuery(Guid ArtefactId) : IRequest<Artefact?>;
