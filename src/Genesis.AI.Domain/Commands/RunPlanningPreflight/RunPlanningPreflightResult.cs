using Genesis.AI.Domain.Planning;

namespace Genesis.AI.Domain.Commands.RunPlanningPreflight;

public sealed record RunPlanningPreflightResult(
    RunPlanningPreflightStatus Status,
    bool PreflightPassed,
    IReadOnlyList<string> Errors,
    IReadOnlyList<PlanningArtefactSummary> OutputArtefacts,
    string? ErrorDetail);
