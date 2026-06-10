using MediatR;

namespace Genesis.AI.Domain.Commands.SplitPlanningTasks;

public sealed record SplitPlanningTasksCommand(Guid ProjectId, string UserId)
    : IRequest<SplitPlanningTasksResult>;
