using Genesis.AI.Domain.Enums;
using MediatR;

namespace Genesis.AI.Domain.Commands.UpdateProject;

public sealed record UpdateProjectCommand(
    Guid ProjectId,
    string Name,
    string? Description,
    string TimeSheetCode,
    ComplianceDomain ComplianceDomain,
    string? GitHubApiRepoUrl,
    string? GitHubAppRepoUrl,
    string? GitHubRepoOwner,
    string? GitHubRepoName,
    string? GitHubInstallationId,
    string? FigmaFileUrl,
    string? FigmaPat,
    string? ReleaseType,
    bool? AssuranceRequired,
    string? PilotDeploymentProcess,
    bool? CsoRoleAssigned,
    bool? IgOwnerRoleAssigned,
    bool? SecurityReviewerAssigned,
    bool? MedicalDeviceFlag,
    string UpdatedBy) : IRequest<UpdateProjectResult>
{
    public string UserErn => UpdatedBy;

    public UpdateProjectCommand(
        Guid projectId,
        string userErn,
        string? gitHubApiRepoUrl,
        string? gitHubAppRepoUrl,
        string? gitHubRepoOwner,
        string? gitHubRepoName,
        string? gitHubInstallationId,
        string? figmaFileUrl,
        string? figmaPat,
        string? releaseType,
        bool? assuranceRequired,
        string? pilotDeploymentProcess,
        bool? csoRoleAssigned,
        bool? igOwnerRoleAssigned,
        bool? securityReviewerAssigned,
        bool? medicalDeviceFlag)
        : this(
            projectId,
            string.Empty,
            null,
            string.Empty,
            default,
            gitHubApiRepoUrl,
            gitHubAppRepoUrl,
            gitHubRepoOwner,
            gitHubRepoName,
            gitHubInstallationId,
            figmaFileUrl,
            figmaPat,
            releaseType,
            assuranceRequired,
            pilotDeploymentProcess,
            csoRoleAssigned,
            igOwnerRoleAssigned,
            securityReviewerAssigned,
            medicalDeviceFlag,
            userErn)
    {
    }

    public UpdateProjectCommand(
        Guid projectId,
        string name,
        string? description,
        string timeSheetCode,
        ComplianceDomain complianceDomain,
        string? gitHubApiRepoUrl,
        string? gitHubAppRepoUrl,
        string? gitHubRepoOwner,
        string? gitHubRepoName,
        string? gitHubInstallationId,
        string? figmaFileUrl,
        string? figmaPat,
        string? releaseType,
        bool? assuranceRequired,
        string? pilotDeploymentProcess,
        bool? csoRoleAssigned,
        bool? igOwnerRoleAssigned,
        bool? securityReviewerAssigned,
        string updatedBy)
        : this(
            projectId,
            name,
            description,
            timeSheetCode,
            complianceDomain,
            gitHubApiRepoUrl,
            gitHubAppRepoUrl,
            gitHubRepoOwner,
            gitHubRepoName,
            gitHubInstallationId,
            figmaFileUrl,
            figmaPat,
            releaseType,
            assuranceRequired,
            pilotDeploymentProcess,
            csoRoleAssigned,
            igOwnerRoleAssigned,
            securityReviewerAssigned,
            null,
            updatedBy)
    {
    }
}
