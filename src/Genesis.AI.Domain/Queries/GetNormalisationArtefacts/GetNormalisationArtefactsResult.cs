using Genesis.AI.Domain.Normalisation;

namespace Genesis.AI.Domain.Queries.GetNormalisationArtefacts;

public sealed record GetNormalisationArtefactsResult(
    bool Found,
    IReadOnlyList<NormalisationArtefactSummary> Artefacts);
