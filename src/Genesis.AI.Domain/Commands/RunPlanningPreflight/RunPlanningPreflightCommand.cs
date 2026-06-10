using MediatR;

namespace Genesis.AI.Domain.Commands.RunPlanningPreflight;

public sealed record RunPlanningPreflightCommand(Guid ProjectId, string UserId)
    : IRequest<RunPlanningPreflightResult>;
