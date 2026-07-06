using Genesis.AI.Domain.AggregatesModel.UiDeltaAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Genesis.AI.Infrastructure.EntityConfigurations;

public sealed class UiDeltaEntityTypeConfiguration : IEntityTypeConfiguration<UiDelta>
{
    public void Configure(EntityTypeBuilder<UiDelta> builder)
    {
        builder.ToTable("ui_delta");

        builder.HasKey(delta => delta.Id);

        builder.Property(delta => delta.Id)
            .HasColumnName("ui_delta_id")
            .ValueGeneratedNever();

        builder.Property(delta => delta.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(delta => delta.StageId)
            .HasColumnName("stage_id")
            .IsRequired();

        builder.Property(delta => delta.RequirementId)
            .HasColumnName("requirement_id")
            .HasMaxLength(100);

        builder.Property(delta => delta.TargetId)
            .HasColumnName("target_id")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(delta => delta.FilePath)
            .HasColumnName("file_path")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(delta => delta.OperationType)
            .HasColumnName("operation_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(delta => delta.SourceType)
            .HasColumnName("source_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(delta => delta.UserRequest)
            .HasColumnName("user_request");

        builder.Property(delta => delta.BeforeSummary)
            .HasColumnName("before_summary")
            .IsRequired();

        builder.Property(delta => delta.AfterSummary)
            .HasColumnName("after_summary")
            .IsRequired();

        builder.Property(delta => delta.RequirementImpact)
            .HasColumnName("requirement_impact")
            .HasColumnType("requirement_impact")
            .IsRequired();

        builder.Property(delta => delta.ConversationId)
            .HasColumnName("conversation_id");

        builder.Property(delta => delta.MessageId)
            .HasColumnName("message_id");

        builder.Property(delta => delta.LockBatchId)
            .HasColumnName("lock_batch_id");

        builder.Property(delta => delta.LockedRequirementFilePath)
            .HasColumnName("locked_requirement_file_path")
            .HasMaxLength(500);

        builder.Property(delta => delta.LockedAt)
            .HasColumnName("locked_at");

        builder.Property(delta => delta.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(delta => delta.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(delta => new { delta.ProjectId, delta.RequirementId, delta.LockedAt })
            .HasDatabaseName("idx_ui_delta_project_requirement_locked");

        builder.HasIndex(delta => delta.StageId)
            .HasDatabaseName("idx_ui_delta_stage_id");
    }
}
