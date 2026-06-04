using Genesis.AI.Domain.AggregatesModel.ProjectDecisionAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Genesis.AI.Infrastructure.EntityConfigurations;

public class ProjectDecisionEntityTypeConfiguration : IEntityTypeConfiguration<ProjectDecision>
{
    public void Configure(EntityTypeBuilder<ProjectDecision> builder)
    {
        builder.ToTable("project_decision");

        builder.HasKey(decision => decision.Id);

        builder.Property(decision => decision.Id)
            .HasColumnName("project_decision_id")
            .ValueGeneratedNever();

        builder.Property(decision => decision.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(decision => decision.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(decision => decision.Context)
            .HasColumnName("context")
            .IsRequired();

        builder.Property(decision => decision.Decision)
            .HasColumnName("decision")
            .IsRequired();

        builder.Property(decision => decision.Consequences)
            .HasColumnName("consequences")
            .IsRequired();

        builder.Property(decision => decision.AuthorErn)
            .HasColumnName("author_ern")
            .HasMaxLength(200);

        builder.Property(decision => decision.AuthorGivenName)
            .HasColumnName("author_given_name")
            .HasMaxLength(100);

        builder.Property(decision => decision.AuthorFamilyName)
            .HasColumnName("author_family_name")
            .HasMaxLength(100);

        builder.Property(decision => decision.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(decision => decision.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(decision => decision.ProjectId)
            .HasDatabaseName("idx_project_decision_project_id");
    }
}
