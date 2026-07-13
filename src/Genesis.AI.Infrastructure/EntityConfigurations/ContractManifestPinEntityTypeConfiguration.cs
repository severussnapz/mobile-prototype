using Genesis.AI.Domain.AggregatesModel.ContractManifestAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Genesis.AI.Infrastructure.EntityConfigurations;

public class ContractManifestPinEntityTypeConfiguration : IEntityTypeConfiguration<ContractManifestPin>
{
    public void Configure(EntityTypeBuilder<ContractManifestPin> builder)
    {
        builder.ToTable("contract_manifest_pin");

        builder.HasKey(contractManifestPin => contractManifestPin.Id);

        builder.Property(contractManifestPin => contractManifestPin.Id)
            .HasColumnName("contract_manifest_pin_id")
            .ValueGeneratedNever();

        builder.Property(contractManifestPin => contractManifestPin.ManifestId)
            .HasColumnName("manifest_id")
            .IsRequired();

        builder.Property(contractManifestPin => contractManifestPin.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(contractManifestPin => contractManifestPin.FilePath)
            .HasColumnName("file_path")
            .IsRequired();

        builder.Property(contractManifestPin => contractManifestPin.PinnedVersion)
            .HasColumnName("pinned_version")
            .IsRequired();
    }
}