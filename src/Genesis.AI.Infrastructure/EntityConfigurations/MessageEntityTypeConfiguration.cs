using System.Text.Json;
using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Genesis.AI.Infrastructure.EntityConfigurations;

public class MessageEntityTypeConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("message");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Id)
            .HasColumnName("message_id")
            .ValueGeneratedNever();

        builder.Property(message => message.ConversationId)
            .HasColumnName("conversation_id")
            .IsRequired();

        builder.Property(message => message.Role)
            .HasColumnName("role")
            .IsRequired();

        builder.Property(message => message.Content)
            .HasColumnName("content")
            .IsRequired();

        builder.Property(message => message.TokenCount)
            .HasColumnName("token_count");

        builder.Property(message => message.UserErn)
            .HasColumnName("user_ern")
            .HasMaxLength(200);

        builder.Property(message => message.GivenName)
            .HasColumnName("given_name")
            .HasMaxLength(100);

        builder.Property(message => message.FamilyName)
            .HasColumnName("family_name")
            .HasMaxLength(100);

        builder.Property(message => message.Images)
            .HasColumnName("images")
            .HasColumnType("jsonb")
            .HasConversion(
                new ValueConverter<List<MessageImage>?, string?>(
                    images => images == null ? null : JsonSerializer.Serialize(images, JsonSerializerOptions.Default),
                    json => json == null ? null : JsonSerializer.Deserialize<List<MessageImage>>(json, JsonSerializerOptions.Default)));

        builder.Property(message => message.Documents)
            .HasColumnName("documents")
            .HasColumnType("jsonb")
            .HasConversion(
                new ValueConverter<List<MessageDocument>?, string?>(
                    documents => documents == null ? null : JsonSerializer.Serialize(documents, JsonSerializerOptions.Default),
                    json => json == null ? null : JsonSerializer.Deserialize<List<MessageDocument>>(json, JsonSerializerOptions.Default)));

        builder.Property(message => message.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
    }
}
