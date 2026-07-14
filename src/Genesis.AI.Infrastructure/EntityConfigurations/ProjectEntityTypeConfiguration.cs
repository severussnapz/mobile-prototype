using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Genesis.AI.Infrastructure.EntityConfigurations;

public class ProjectEntityTypeConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("project");

        builder.HasKey(project => project.Id);

        builder.Property(project => project.Id)
            .HasColumnName("project_id")
            .ValueGeneratedNever();

        builder.Property(project => project.Code)
            .HasColumnName("code")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(project => project.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(project => project.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);

        builder.Property(project => project.TimeSheetCode)
            .HasColumnName("time_sheet_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(project => project.ComplianceDomain)
            .HasColumnName("compliance_domain")
            .IsRequired();

        builder.Property(project => project.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(project => project.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(project => project.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(project => project.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(project => project.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired();

        builder.Property(project => project.GitHubApiRepoUrl)
            .HasColumnName("github_api_repo_url")
            .HasMaxLength(500);

        builder.Property(project => project.GitHubAppRepoUrl)
            .HasColumnName("github_app_repo_url")
            .HasMaxLength(500);

        builder.Property(project => project.GitHubRepoOwner)
            .HasColumnName("github_repo_owner")
            .HasMaxLength(200);

        builder.Property(project => project.GitHubRepoName)
            .HasColumnName("github_repo_name")
            .HasMaxLength(200);

        builder.Property(project => project.GitHubInstallationId)
            .HasColumnName("github_installation_id")
            .HasMaxLength(100);

        builder.Property(project => project.FigmaFileUrl)
            .HasColumnName("figma_file_url")
            .HasMaxLength(500);

        builder.Property(project => project.FigmaPatEncrypted)
            .HasColumnName("figma_pat_encrypted");

        builder.Property(project => project.ReleaseType)
            .HasColumnName("release_type")
            .HasMaxLength(50);

        builder.Property(project => project.AssuranceRequired)
            .HasColumnName("assurance_required");

        builder.Property(project => project.PilotDeploymentProcess)
            .HasColumnName("pilot_deployment_process");

        builder.Property(project => project.CsoRoleAssigned)
            .HasColumnName("cso_role_assigned");

        builder.Property(project => project.IgOwnerRoleAssigned)
            .HasColumnName("ig_owner_role_assigned");

        builder.Property(project => project.SecurityReviewerAssigned)
            .HasColumnName("security_reviewer_assigned");

        builder.Property(project => project.MedicalDeviceFlag)
            .HasColumnName("medical_device_flag");

        builder.HasIndex(project => project.Code)
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasMany(project => project.PipelineStages)
            .WithOne()
            .HasForeignKey(stage => stage.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(project => !project.IsDeleted);

        builder.Ignore(project => project.DomainEvents);
    }
}
