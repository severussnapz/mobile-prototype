using Genesis.AI.Domain.Normalisation;

namespace Genesis.AI.Domain.Queries.GetNormalisationStatus;

public sealed record GetNormalisationStatusResult(
    bool Found,
    string RunStatus,
    DateTimeOffset? LastRunAtUtc,
    IReadOnlyList<string> RunErrors,
    bool GatePassed,
    bool PlanningEligible,
    bool BypassActive,
    string? BypassedBy,
    DateTimeOffset? BypassedAtUtc,
    IReadOnlyList<string> GateErrors,
    IReadOnlyList<NormalisationArtefactSummary> OutputArtefacts);
