namespace Genesis.AI.Domain.Normalisation;

public sealed record NormalisationGateEvaluation(
    bool RunPrerequisitesMet,
    bool GatePassed,
    IReadOnlyList<string> Errors,
    IReadOnlyList<NormalisationArtefactSummary> OutputArtefacts);
