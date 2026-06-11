namespace Genesis.AI.Domain.Planning;

public sealed record PlanningGateEvaluation(
    bool RunPrerequisitesMet,
    bool PreflightPassed,
    bool TaskPlanExists,
    bool TasksDataExists,
    bool EmApproved,
    bool EmApprovalIsStale,
    bool SplitPassed,
    bool GatePassed,
    IReadOnlyList<string> Errors,
    IReadOnlyList<PlanningArtefactSummary> OutputArtefacts);
