using MediatR;

namespace Genesis.AI.Domain.Commands.UpdateProjectP00;

public sealed record UpdateProjectP00Command(
    Guid ProjectId,
    string TriggeredBy,
    string? ReleaseType,
    bool? AssuranceRequired,
    string? PilotDeploymentProcess,
    bool? CsoRoleAssigned,
    bool? IgOwnerRoleAssigned,
    bool? SecurityReviewerAssigned,
    bool? MedicalDeviceFlag
) : IRequest<Unit>;
