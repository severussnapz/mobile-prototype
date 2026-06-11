using Genesis.AI.Domain.Planning;

namespace Genesis.AI.Domain.Queries.GetPlanningStatus;

public sealed record GetPlanningStatusResult(
    bool Found,
    bool PreflightPassed,
    DateTimeOffset? LastPreflightAtUtc,
    IReadOnlyList<string> PreflightErrors,
    bool TaskPlanExists,
    bool TasksDataExists,
    bool EmApproved,
    bool EmApprovalIsStale,
    string? ApprovedBy,
    DateTimeOffset? ApprovedAtUtc,
    bool SplitPassed,
    int TaskCount,
    bool GatePassed,
    IReadOnlyList<string> GateErrors,
    IReadOnlyList<PlanningArtefactSummary> OutputArtefacts);
