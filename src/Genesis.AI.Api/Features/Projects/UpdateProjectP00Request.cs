namespace Genesis.AI.Api.Features.Projects;

public sealed class UpdateProjectP00Request
{
    public string? ReleaseType { get; init; }
    public bool? AssuranceRequired { get; init; }
    public string? PilotDeploymentProcess { get; init; }
    public bool? CsoRoleAssigned { get; init; }
    public bool? IgOwnerRoleAssigned { get; init; }
    public bool? SecurityReviewerAssigned { get; init; }
    public bool? MedicalDeviceFlag { get; init; }
}
