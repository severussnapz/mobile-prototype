using System.Globalization;
using System.Text;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services.GitHub;

public sealed class ProjectMarkdownGenerator : IProjectMarkdownGenerator
{
    public string Generate(Project project)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Project Configuration");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Name: {project.Name}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Code: {project.Code}");

        if (project.ReleaseType is not null)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"Release Type: {project.ReleaseType}");
        }

        if (project.AssuranceRequired is not null)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"Assurance Required: {(project.AssuranceRequired.Value ? "Yes" : "No")}");
        }

        if (project.CsoRoleAssigned is not null)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"CSO Role Assigned: {(project.CsoRoleAssigned.Value ? "Yes" : "No")}");
        }

        if (project.IgOwnerRoleAssigned is not null)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"IG Owner Role Assigned: {(project.IgOwnerRoleAssigned.Value ? "Yes" : "No")}");
        }

        if (project.SecurityReviewerAssigned is not null)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"Security Reviewer Assigned: {(project.SecurityReviewerAssigned.Value ? "Yes" : "No")}");
        }

        if (project.MedicalDeviceFlag is not null)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"Medical Device: {(project.MedicalDeviceFlag.Value ? "Yes" : "No")}");
        }

        if (project.PilotDeploymentProcess is not null)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"Pilot/Deployment Process: {project.PilotDeploymentProcess}");
        }

        return sb.ToString();
    }
}
