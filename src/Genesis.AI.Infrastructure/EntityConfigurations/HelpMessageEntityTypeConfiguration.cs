using Genesis.AI.Domain.AggregatesModel.HelpChatAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Genesis.AI.Infrastructure.EntityConfigurations;

public class HelpMessageEntityTypeConfiguration : IEntityTypeConfiguration<HelpMessage>
{
    public void Configure(EntityTypeBuilder<HelpMessage> builder)
    {
        builder.ToTable("help_message");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .HasColumnName("help_message_uuid")
            .ValueGeneratedNever();
        builder.Property(m => m.HelpConversationId)
            .HasColumnName("help_conversation_id")
            .IsRequired();
        builder.Property(m => m.Role).HasColumnName("role").HasMaxLength(20).IsRequired();
        builder.Property(m => m.Content).HasColumnName("content").IsRequired();
        builder.Property(m => m.CreatedAt).HasColumnName("created_at").IsRequired();
    }
}
