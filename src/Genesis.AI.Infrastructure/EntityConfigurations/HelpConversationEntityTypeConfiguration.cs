using Genesis.AI.Domain.AggregatesModel.HelpChatAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Genesis.AI.Infrastructure.EntityConfigurations;

public class HelpConversationEntityTypeConfiguration : IEntityTypeConfiguration<HelpConversation>
{
    public void Configure(EntityTypeBuilder<HelpConversation> builder)
    {
        builder.ToTable("help_conversation");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id)
            .HasColumnName("help_conversation_uuid")
            .ValueGeneratedNever();
        builder.Property(h => h.ProjectId).HasColumnName("project_id");
        builder.Property(h => h.UserErn).HasColumnName("user_ern").HasMaxLength(100).IsRequired();
        builder.Property(h => h.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(h => h.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.HasMany(h => h.Messages)
            .WithOne()
            .HasForeignKey(m => m.HelpConversationId)
            .HasConstraintName("help_message_help_conversation_id_fkey");
    }
}
