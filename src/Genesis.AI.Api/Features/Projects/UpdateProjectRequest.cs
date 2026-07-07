namespace Genesis.AI.Api.Features.Projects;

public sealed class UpdateProjectRequest
{
    public string? GitHubApiRepoUrl { get; init; }
    public string? GitHubAppRepoUrl { get; init; }
    public string? GitHubInstallationId { get; init; }
    public string? FigmaFileUrl { get; init; }
    public string? FigmaPat { get; init; }
    public string? ReleaseType { get; init; }
    public bool? AssuranceRequired { get; init; }
    public string? PilotDeploymentProcess { get; init; }
    public bool? CsoRoleAssigned { get; init; }
    public bool? IgOwnerRoleAssigned { get; init; }
    public bool? SecurityReviewerAssigned { get; init; }
    public bool? MedicalDeviceFlag { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? TimeSheetCode { get; init; }
    public string? ComplianceDomain { get; init; }
}
