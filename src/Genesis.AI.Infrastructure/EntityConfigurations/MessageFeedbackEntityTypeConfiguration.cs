using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Genesis.AI.Infrastructure.EntityConfigurations;

public class MessageFeedbackEntityTypeConfiguration : IEntityTypeConfiguration<MessageFeedback>
{
    public void Configure(EntityTypeBuilder<MessageFeedback> builder)
    {
        builder.ToTable("conversation_message_feedback");

        builder.HasKey(feedback => feedback.Id);
        builder.Property(feedback => feedback.Id).HasColumnName("conversation_message_feedback_id");

        builder.Property(feedback => feedback.ConversationId)
            .HasColumnName("conversation_id")
            .IsRequired();

        builder.Property(feedback => feedback.MessageId)
            .HasColumnName("message_id")
            .IsRequired();

        builder.Property(feedback => feedback.StageType)
            .HasColumnName("stage_type")
            .IsRequired();

        builder.Property(feedback => feedback.IsHelpful)
            .HasColumnName("is_helpful")
            .IsRequired();

        builder.Property(feedback => feedback.Reason)
            .HasColumnName("reason")
            .HasColumnType("text");

        builder.Property(feedback => feedback.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(feedback => feedback.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(feedback => feedback.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(feedback => new { feedback.MessageId, feedback.CreatedBy })
            .IsUnique();
        builder.HasIndex(feedback => new { feedback.StageType, feedback.CreatedAt });
        builder.HasIndex(feedback => feedback.ConversationId);

        builder.HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(feedback => feedback.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Message>()
            .WithMany()
            .HasForeignKey(feedback => feedback.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}