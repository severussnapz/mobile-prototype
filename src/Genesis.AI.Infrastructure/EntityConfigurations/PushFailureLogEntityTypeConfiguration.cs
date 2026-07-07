using Genesis.AI.Domain.AggregatesModel.PushFailureLogAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Genesis.AI.Infrastructure.EntityConfigurations;

public sealed class PushFailureLogEntityTypeConfiguration
    : IEntityTypeConfiguration<PushFailureLog>
{
    public void Configure(EntityTypeBuilder<PushFailureLog> builder)
    {
        builder.ToTable("push_failure_log");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("push_failure_log_uuid")
            .ValueGeneratedNever();

        builder.Property(p => p.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(p => p.ArtefactId)
            .HasColumnName("artefact_id")
            .IsRequired();

        builder.Property(p => p.FilePath)
            .HasColumnName("file_path")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(p => p.ErrorMessage)
            .HasColumnName("error_message")
            .IsRequired();

        builder.Property(p => p.FailedAt)
            .HasColumnName("failed_at")
            .IsRequired();

        builder.Property(p => p.RetryCount)
            .HasColumnName("retry_count")
            .IsRequired();

        builder.Property(p => p.ResolvedAt)
            .HasColumnName("resolved_at");
    }
}
