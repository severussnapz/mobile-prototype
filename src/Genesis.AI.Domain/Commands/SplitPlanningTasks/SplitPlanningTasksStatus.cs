namespace Genesis.AI.Domain.Commands.SplitPlanningTasks;

public enum SplitPlanningTasksStatus
{
    Success = 0,
    ProjectNotFound = 1,
    TasksDataMissing = 2,
    EmApprovalMissing = 3,
    EmApprovalStale = 4,
    InvalidTasksData = 5,
    DuplicateTaskIds = 6,
    DuplicateCheckAssignments = 7
}
