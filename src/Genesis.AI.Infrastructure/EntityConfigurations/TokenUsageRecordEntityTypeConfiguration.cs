using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Genesis.AI.Infrastructure.EntityConfigurations;

public class TokenUsageRecordEntityTypeConfiguration : IEntityTypeConfiguration<TokenUsageRecord>
{
    public void Configure(EntityTypeBuilder<TokenUsageRecord> builder)
    {
        builder.ToTable("token_usage");

        builder.HasKey(record => record.Id);

        builder.Property(record => record.Id)
            .HasColumnName("token_usage_id")
            .ValueGeneratedNever();

        builder.Property(record => record.ConversationId)
            .HasColumnName("conversation_id")
            .IsRequired();

        builder.Property(record => record.InputTokens)
            .HasColumnName("input_tokens")
            .IsRequired();

        builder.Property(record => record.OutputTokens)
            .HasColumnName("output_tokens")
            .IsRequired();

        builder.Property(record => record.CacheReadInputTokens)
            .HasColumnName("cache_read_input_tokens")
            .IsRequired();

        builder.Property(record => record.CacheWriteInputTokens)
            .HasColumnName("cache_write_input_tokens")
            .IsRequired();

        builder.Property(record => record.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(record => record.ConversationId)
            .HasDatabaseName("idx_token_usage_conversation_id");
    }
}
