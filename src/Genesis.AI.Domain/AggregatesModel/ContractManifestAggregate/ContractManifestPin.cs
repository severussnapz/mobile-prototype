using Genesis.AI.Core.Domain;
using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Domain.AggregatesModel.ContractManifestAggregate;

public class ContractManifestPin : Entity
{
    public Guid ManifestId { get; private set; }
    public ContractPinRole Role { get; private set; }
    public string FilePath { get; private set; } = null!;
    public int PinnedVersion { get; private set; }

    private ContractManifestPin() { } // Required for EF Core

    public static ContractManifestPin Create(ContractPinRole role, string filePath, int pinnedVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (pinnedVersion <= 0)
        {
            throw new ArgumentException("Pinned version must be greater than zero.", nameof(pinnedVersion));
        }

        return new ContractManifestPin
        {
            Id = Guid.NewGuid(),
            Role = role,
            FilePath = filePath,
            PinnedVersion = pinnedVersion
        };
    }
}