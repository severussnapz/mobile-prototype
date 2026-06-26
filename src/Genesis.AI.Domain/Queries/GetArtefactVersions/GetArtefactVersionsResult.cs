using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;

namespace Genesis.AI.Domain.Queries.GetArtefactVersions;

public record GetArtefactVersionsResult(IReadOnlyList<Artefact> Versions);
