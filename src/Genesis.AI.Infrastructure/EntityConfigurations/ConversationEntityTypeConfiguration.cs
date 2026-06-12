using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Genesis.AI.Infrastructure.EntityConfigurations;

public class ConversationEntityTypeConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversation");

        builder.HasKey(conversation => conversation.Id);

        builder.Property(conversation => conversation.Id)
            .HasColumnName("conversation_id")
            .ValueGeneratedNever();

        builder.Property(conversation => conversation.StageId)
            .HasColumnName("stage_id")
            .IsRequired();

        builder.Property(conversation => conversation.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(conversation => conversation.MessageCount)
            .HasColumnName("message_count")
            .IsRequired();

        builder.Property(conversation => conversation.CurrentPhase)
            .HasColumnName("current_phase")
            .IsRequired();

        builder.Property(conversation => conversation.PhaseName)
            .HasColumnName("phase_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(conversation => conversation.TotalPhases)
            .HasColumnName("total_phases")
            .IsRequired();

        builder.Property(conversation => conversation.QuestionsAsked)
            .HasColumnName("questions_asked")
            .IsRequired();

        builder.Property(conversation => conversation.EstimatedTotalQuestions)
            .HasColumnName("estimated_total_questions");

        builder.Property(conversation => conversation.RequirementsCaptured)
            .HasColumnName("requirements_captured")
            .IsRequired();

        builder.Property(conversation => conversation.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(conversation => conversation.ResumedAt)
            .HasColumnName("resumed_at");

        builder.Property(conversation => conversation.RequirementId)
            .HasColumnName("requirement_id")
            .HasMaxLength(50);

        builder.Property(conversation => conversation.OrchestrationMode)
            .HasColumnName("orchestration_mode")
            .IsRequired()
            .HasDefaultValueSql("'forward_sweep'::orchestration_mode");

        builder.Property(conversation => conversation.ContinuedFromConversationId)
            .HasColumnName("continued_from_conversation_id");

        builder.HasMany(conversation => conversation.Messages)
            .WithOne()
            .HasForeignKey(message => message.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(conversation => conversation.ParkingLotItems)
            .WithOne()
            .HasForeignKey(parkingLotItem => parkingLotItem.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(conversation => conversation.TokenUsageRecords)
            .WithOne()
            .HasForeignKey(record => record.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(conversation => conversation.Messages)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(conversation => conversation.ParkingLotItems)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(conversation => conversation.TokenUsageRecords)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
