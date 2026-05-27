using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Genesis.AI.Infrastructure.EntityConfigurations;

public class PipelineStageEntityTypeConfiguration : IEntityTypeConfiguration<PipelineStage>
{
    public void Configure(EntityTypeBuilder<PipelineStage> builder)
    {
        builder.ToTable("pipeline_stage");

        builder.HasKey(stage => stage.Id);

        builder.Property(stage => stage.Id)
            .HasColumnName("pipeline_stage_id")
            .ValueGeneratedNever();

        builder.Property(stage => stage.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(stage => stage.StageType)
            .HasColumnName("stage_type")
            .IsRequired();

        builder.Property(stage => stage.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(stage => stage.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();

        builder.Property(stage => stage.Iteration)
            .HasColumnName("iteration")
            .IsRequired();

        builder.Property(stage => stage.StartedAt)
            .HasColumnName("started_at");

        builder.Property(stage => stage.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(stage => stage.CompletedBy)
            .HasColumnName("completed_by");

        builder.HasIndex(stage => stage.ProjectId);

        builder.Ignore(stage => stage.DomainEvents);
    }
}
