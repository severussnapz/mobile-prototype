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

        builder.HasIndex(project => project.Code)
            .IsUnique();

        builder.HasMany(project => project.PipelineStages)
            .WithOne()
            .HasForeignKey(stage => stage.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(project => !project.IsDeleted);

        builder.Ignore(project => project.DomainEvents);
    }
}
