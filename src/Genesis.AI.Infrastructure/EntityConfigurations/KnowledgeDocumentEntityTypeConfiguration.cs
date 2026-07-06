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

        builder.HasKey(doc => doc.Id);

        builder.Property(doc => doc.Id)
            .HasColumnName("knowledge_document_uuid")
            .ValueGeneratedNever();

        builder.Property(doc => doc.Namespace)
            .HasColumnName("namespace")
            .IsRequired();

        builder.Property(doc => doc.ProjectId)
            .HasColumnName("project_id");

        builder.Property(doc => doc.SourcePath)
            .HasColumnName("source_path")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(doc => doc.ChunkIndex)
            .HasColumnName("chunk_index")
            .IsRequired();

        builder.Property(doc => doc.Content)
            .HasColumnName("content")
            .IsRequired();

        builder.Property(doc => doc.Embedding)
            .HasColumnName("embedding")
            .IsRequired();

        builder.Property(doc => doc.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new())
            .IsRequired();

        builder.Property(doc => doc.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(doc => doc.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(doc => doc.Namespace)
            .HasDatabaseName("idx_knowledge_document_namespace");

        builder.HasIndex(doc => doc.ProjectId)
            .HasDatabaseName("idx_knowledge_document_project")
            .HasFilter("project_id IS NOT NULL");

        builder.HasIndex(doc => new { doc.Namespace, doc.SourcePath })
            .HasDatabaseName("idx_knowledge_document_source");

        builder.HasIndex(doc => new { doc.Namespace, doc.SourcePath, doc.ProjectId, doc.ChunkIndex })
            .HasDatabaseName("uq_knowledge_document_chunk")
            .IsUnique();
    }
}
