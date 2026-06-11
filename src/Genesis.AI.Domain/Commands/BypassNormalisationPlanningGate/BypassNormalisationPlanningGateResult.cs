namespace Genesis.AI.Domain.Commands.BypassNormalisationPlanningGate;

public sealed record BypassNormalisationPlanningGateResult(
    BypassNormalisationPlanningGateStatus Status,
    string? ErrorDetail);
