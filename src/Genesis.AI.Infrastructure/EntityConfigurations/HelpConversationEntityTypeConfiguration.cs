using Genesis.AI.Domain.AggregatesModel.HelpChatAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Genesis.AI.Infrastructure.EntityConfigurations;

public class HelpConversationEntityTypeConfiguration : IEntityTypeConfiguration<HelpConversation>
{
    public void Configure(EntityTypeBuilder<HelpConversation> builder)
    {
        builder.ToTable("help_conversation");
        builder.HasKey(helpConversation => helpConversation.Id);
        builder.Property(helpConversation => helpConversation.Id)
            .HasColumnName("help_conversation_uuid")
            .ValueGeneratedNever();
        builder.Property(helpConversation => helpConversation.ProjectId).HasColumnName("project_id");
        builder.Property(helpConversation => helpConversation.UserErn).HasColumnName("user_ern").HasMaxLength(100).IsRequired();
        builder.Property(helpConversation => helpConversation.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(helpConversation => helpConversation.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.HasMany(helpConversation => helpConversation.Messages)
            .WithOne()
            .HasForeignKey(helpMessage => helpMessage.HelpConversationId)
            .HasConstraintName("help_message_help_conversation_id_fkey");
    }
}
