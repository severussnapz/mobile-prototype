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
        builder.HasKey(knowledgeDocument => knowledgeDocument.Id);
        builder.Property(knowledgeDocument => knowledgeDocument.Id)
            .HasColumnName("knowledge_document_uuid")
            .ValueGeneratedNever();
        builder.Property(knowledgeDocument => knowledgeDocument.Namespace)
            .HasColumnName("namespace")
            .IsRequired();
        builder.Property(knowledgeDocument => knowledgeDocument.ProjectId)
            .HasColumnName("project_id");
        builder.Property(knowledgeDocument => knowledgeDocument.SourcePath)
            .HasColumnName("source_path")
            .HasMaxLength(500)
            .IsRequired();
        builder.Property(knowledgeDocument => knowledgeDocument.ChunkIndex)
            .HasColumnName("chunk_index")
            .IsRequired();
        builder.Property(knowledgeDocument => knowledgeDocument.Content)
            .HasColumnName("content")
            .IsRequired();
        builder.Property(knowledgeDocument => knowledgeDocument.Embedding)
            .HasColumnName("embedding")
            .IsRequired();
        builder.Property(knowledgeDocument => knowledgeDocument.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .HasConversion(
                metadata => JsonSerializer.Serialize(metadata, (JsonSerializerOptions?)null),
                serializedMetadata => JsonSerializer.Deserialize<Dictionary<string, string>>(serializedMetadata, (JsonSerializerOptions?)null) ?? new())
            .IsRequired();
        builder.Property(knowledgeDocument => knowledgeDocument.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
        builder.Property(knowledgeDocument => knowledgeDocument.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();
    }
}
