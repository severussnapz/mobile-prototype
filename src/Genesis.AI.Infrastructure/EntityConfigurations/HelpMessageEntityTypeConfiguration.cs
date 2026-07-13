using Genesis.AI.Domain.AggregatesModel.HelpChatAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Genesis.AI.Infrastructure.EntityConfigurations;

public class HelpMessageEntityTypeConfiguration : IEntityTypeConfiguration<HelpMessage>
{
    public void Configure(EntityTypeBuilder<HelpMessage> builder)
    {
        builder.ToTable("help_message");
        builder.HasKey(helpMessage => helpMessage.Id);
        builder.Property(helpMessage => helpMessage.Id)
            .HasColumnName("help_message_uuid")
            .ValueGeneratedNever();
        builder.Property(helpMessage => helpMessage.HelpConversationId)
            .HasColumnName("help_conversation_id")
            .IsRequired();
        builder.Property(helpMessage => helpMessage.Role).HasColumnName("role").HasMaxLength(20).IsRequired();
        builder.Property(helpMessage => helpMessage.Content).HasColumnName("content").IsRequired();
        builder.Property(helpMessage => helpMessage.CreatedAt).HasColumnName("created_at").IsRequired();
    }
}
