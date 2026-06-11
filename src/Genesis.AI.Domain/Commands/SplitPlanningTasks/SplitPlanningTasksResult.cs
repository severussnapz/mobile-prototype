using Genesis.AI.Domain.Planning;

namespace Genesis.AI.Domain.Commands.SplitPlanningTasks;

public sealed record SplitPlanningTasksResult(
    SplitPlanningTasksStatus Status,
    int TaskCount,
    IReadOnlyList<string> DuplicateTaskIds,
    IReadOnlyList<string> DuplicateCheckAssignments,
    IReadOnlyList<PlanningArtefactSummary> OutputArtefacts,
    string? ErrorDetail);
