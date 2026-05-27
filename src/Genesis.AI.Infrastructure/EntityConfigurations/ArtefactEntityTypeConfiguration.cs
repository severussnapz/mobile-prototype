using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Genesis.AI.Infrastructure.EntityConfigurations;

public class ArtefactEntityTypeConfiguration : IEntityTypeConfiguration<Artefact>
{
    public void Configure(EntityTypeBuilder<Artefact> builder)
    {
        builder.ToTable("artefact");

        builder.HasKey(artefact => artefact.Id);

        builder.Property(artefact => artefact.Id)
            .HasColumnName("artefact_id")
            .ValueGeneratedNever();

        builder.Property(artefact => artefact.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(artefact => artefact.Version)
            .HasColumnName("version")
            .IsRequired();

        builder.Property(artefact => artefact.FilePath)
            .HasColumnName("file_path")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(artefact => artefact.S3Key)
            .HasColumnName("s3_key")
            .HasMaxLength(1000);

        builder.Property(artefact => artefact.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(artefact => artefact.Content)
            .HasColumnName("content");

        builder.Property(artefact => artefact.SizeBytes)
            .HasColumnName("size_bytes");

        builder.Property(artefact => artefact.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(artefact => artefact.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
    }
}
