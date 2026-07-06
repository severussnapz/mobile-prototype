using Genesis.AI.Domain.AggregatesModel.KnowledgeAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Genesis.AI.Infrastructure.EntityConfigurations;

public class KnowledgeDocumentEntityTypeConfiguration : IEntityTypeConfiguration<KnowledgeDocument>
{
    public void Configure(EntityTypeBuilder<KnowledgeDocument> builder)
    {
        builder.Property(doc => doc.Metadata)
            .HasColumnType("jsonb")
            .IsRequired();
    }
}
