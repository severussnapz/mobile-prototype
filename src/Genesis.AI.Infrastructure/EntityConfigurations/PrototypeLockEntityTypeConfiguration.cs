using Genesis.AI.Domain.AggregatesModel.PrototypeLockAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Genesis.AI.Infrastructure.EntityConfigurations;

public sealed class PrototypeLockEntityTypeConfiguration : IEntityTypeConfiguration<PrototypeLock>
{
    public void Configure(EntityTypeBuilder<PrototypeLock> builder)
    {
        builder.ToTable("prototype_lock");

        builder.HasKey(lockRow => lockRow.Id);

        builder.Property(lockRow => lockRow.Id)
            .HasColumnName("prototype_lock_id")
            .ValueGeneratedNever();

        builder.Property(lockRow => lockRow.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(lockRow => lockRow.StageId)
            .HasColumnName("stage_id")
            .IsRequired();

        builder.Property(lockRow => lockRow.LockedAt)
            .HasColumnName("locked_at");

        builder.Property(lockRow => lockRow.LockedBy)
            .HasColumnName("locked_by")
            .HasMaxLength(200);

        builder.Property(lockRow => lockRow.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(lockRow => lockRow.ProjectId)
            .HasDatabaseName("idx_prototype_lock_project_id")
            .IsUnique();

        builder.HasIndex(lockRow => lockRow.StageId)
            .HasDatabaseName("idx_prototype_lock_stage_id")
            .IsUnique();
    }
}
