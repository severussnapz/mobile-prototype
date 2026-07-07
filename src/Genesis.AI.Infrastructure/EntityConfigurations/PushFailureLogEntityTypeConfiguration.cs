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

        builder.HasKey(pushFailureLog => pushFailureLog.Id);

        builder.Property(pushFailureLog => pushFailureLog.Id)
            .HasColumnName("push_failure_log_uuid")
            .ValueGeneratedNever();

        builder.Property(pushFailureLog => pushFailureLog.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(pushFailureLog => pushFailureLog.ArtefactId)
            .HasColumnName("artefact_id")
            .IsRequired();

        builder.Property(pushFailureLog => pushFailureLog.FilePath)
            .HasColumnName("file_path")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(pushFailureLog => pushFailureLog.ErrorMessage)
            .HasColumnName("error_message")
            .IsRequired();

        builder.Property(pushFailureLog => pushFailureLog.FailedAt)
            .HasColumnName("failed_at")
            .IsRequired();

        builder.Property(pushFailureLog => pushFailureLog.RetryCount)
            .HasColumnName("retry_count")
            .IsRequired();

        builder.Property(pushFailureLog => pushFailureLog.ResolvedAt)
            .HasColumnName("resolved_at");
    }
}
