using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using MediatR;

namespace Genesis.AI.Domain.Commands.CreateArtefacts;

public record CreateArtefactsCommand(
    Guid ProjectId,
    string UserId,
    IReadOnlyList<CreateArtefactItem> Artefacts) : IRequest<IReadOnlyList<Artefact>>;
