using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Genesis.AI.Infrastructure.EntityConfigurations;

public sealed class RequirementChangeEntityTypeConfiguration
    : IEntityTypeConfiguration<RequirementChange>
{
    public void Configure(EntityTypeBuilder<RequirementChange> builder)
    {
        builder.ToTable("requirement_changes");

        builder.HasKey(change => change.Id);

        builder.Property(change => change.Id)
            .HasColumnName("id");

        builder.Property(change => change.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(change => change.ReqId)
            .HasColumnName("req_id")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(change => change.ChangeType)
            .HasColumnName("change_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(change => change.RaisingPipeline)
            .HasColumnName("raising_pipeline")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(change => change.RaisingPipelineConversationId)
            .HasColumnName("raising_pipeline_conversation_id");

        builder.Property(change => change.ProposedAcText)
            .HasColumnName("proposed_ac_text");

        builder.Property(change => change.ApprovedAcText)
            .HasColumnName("approved_ac_text");

        builder.Property(change => change.HumanEdited)
            .HasColumnName("human_edited")
            .IsRequired();

        builder.Property(change => change.Rationale)
            .HasColumnName("rationale")
            .IsRequired();

        builder.Property(change => change.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(change => change.ClinicalSafetyImpact)
            .HasColumnName("clinical_safety_impact")
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(change => change.IgImpact)
            .HasColumnName("ig_impact")
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(change => change.SecurityImpact)
            .HasColumnName("security_impact")
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(change => change.ClinicalSafetyReviewed)
            .HasColumnName("clinical_safety_reviewed")
            .IsRequired();

        builder.Property(change => change.ClinicalSafetyReviewer)
            .HasColumnName("clinical_safety_reviewer")
            .HasMaxLength(200);

        builder.Property(change => change.ClinicalSafetyReviewedAt)
            .HasColumnName("clinical_safety_reviewed_at");

        builder.Property(change => change.IgReviewed)
            .HasColumnName("ig_reviewed")
            .IsRequired();

        builder.Property(change => change.IgReviewer)
            .HasColumnName("ig_reviewer")
            .HasMaxLength(200);

        builder.Property(change => change.IgReviewedAt)
            .HasColumnName("ig_reviewed_at");

        builder.Property(change => change.SecurityReviewed)
            .HasColumnName("security_reviewed")
            .IsRequired();

        builder.Property(change => change.SecurityReviewer)
            .HasColumnName("security_reviewer")
            .HasMaxLength(200);

        builder.Property(change => change.SecurityReviewedAt)
            .HasColumnName("security_reviewed_at");

        builder.Property(change => change.PrototypeFragmentsAffected)
            .HasColumnName("prototype_fragments_affected")
            .HasColumnType("text[]");

        builder.Property(change => change.ApprovedBy)
            .HasColumnName("approved_by")
            .HasMaxLength(200);

        builder.Property(change => change.ApprovedAt)
            .HasColumnName("approved_at");

        builder.Property(change => change.UndoneBy)
            .HasColumnName("undone_by")
            .HasMaxLength(200);

        builder.Property(change => change.UndoneAt)
            .HasColumnName("undone_at");

        builder.Property(change => change.UndoRationale)
            .HasColumnName("undo_rationale");

        builder.Property(change => change.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(change => change.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(change => change.ProjectId)
            .HasDatabaseName("idx_requirement_changes_project_id");

        builder.HasIndex(change => new { change.ProjectId, change.ReqId })
            .HasDatabaseName("idx_requirement_changes_project_req");
    }
}
