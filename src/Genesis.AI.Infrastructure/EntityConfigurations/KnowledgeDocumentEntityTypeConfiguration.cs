using Genesis.AI.Domain.AggregatesModel.KnowledgeAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Genesis.AI.Infrastructure.EntityConfigurations;

public class KnowledgeDocumentEntityTypeConfiguration : IEntityTypeConfiguration<KnowledgeDocument>
{
    public void Configure(EntityTypeBuilder<KnowledgeDocument> builder)
    {
        builder.ToTable("knowledge_document");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id)
            .HasColumnName("knowledge_document_uuid")
            .ValueGeneratedNever();
        builder.Property(d => d.Namespace)
            .HasColumnName("namespace")
            .IsRequired();
        builder.Property(d => d.ProjectId)
            .HasColumnName("project_id");
        builder.Property(d => d.SourcePath)
            .HasColumnName("source_path")
            .HasMaxLength(500)
            .IsRequired();
        builder.Property(d => d.ChunkIndex)
            .HasColumnName("chunk_index")
            .IsRequired();
        builder.Property(d => d.Content)
            .HasColumnName("content")
            .IsRequired();
        builder.Property(d => d.Embedding)
            .HasColumnName("embedding")
            .IsRequired();
        builder.Property(d => d.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new())
            .IsRequired();
        builder.Property(d => d.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
        builder.Property(d => d.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();
    }
}
