using MediatR;

namespace Genesis.AI.Domain.Commands.BypassNormalisationPlanningGate;

public sealed record BypassNormalisationPlanningGateCommand(
    Guid ProjectId,
    string UserId,
    string? Reason)
    : IRequest<BypassNormalisationPlanningGateResult>;
