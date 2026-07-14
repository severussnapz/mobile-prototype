using Genesis.AI.Domain.AggregatesModel.ContractManifestAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Genesis.AI.Infrastructure.EntityConfigurations;

public class ContractManifestEntityTypeConfiguration : IEntityTypeConfiguration<ContractManifest>
{
    public void Configure(EntityTypeBuilder<ContractManifest> builder)
    {
        builder.ToTable("contract_manifest");

        builder.HasKey(contractManifest => contractManifest.Id);

        builder.Property(contractManifest => contractManifest.Id)
            .HasColumnName("contract_manifest_id")
            .ValueGeneratedNever();

        builder.Property(contractManifest => contractManifest.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(contractManifest => contractManifest.Version)
            .HasColumnName("version")
            .IsRequired();

        builder.Property(contractManifest => contractManifest.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(contractManifest => contractManifest.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasMany(contractManifest => contractManifest.Pins)
            .WithOne()
            .HasForeignKey(contractManifestPin => contractManifestPin.ManifestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(ContractManifest.Pins))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}