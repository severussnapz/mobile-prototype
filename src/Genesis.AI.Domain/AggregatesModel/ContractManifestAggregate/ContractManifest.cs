using Genesis.AI.Core.Domain;
using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Domain.AggregatesModel.ContractManifestAggregate;

public class ContractManifest : Entity, IAggregateRoot
{
    public Guid ProjectId { get; private set; }
    public int Version { get; private set; }
    public string CreatedBy { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private List<ContractManifestPin> _pins = [];
    public IReadOnlyCollection<ContractManifestPin> Pins => _pins.AsReadOnly();

    private ContractManifest() { } // Required for EF Core

    public static ContractManifest Create(
        Guid projectId,
        int version,
        IEnumerable<ContractManifestPin> pins,
        string createdBy,
        TimeProvider timeProvider)
    {
        if (version <= 0)
        {
            throw new ArgumentException("Version must be greater than zero.", nameof(version));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);
        ArgumentNullException.ThrowIfNull(pins);
        ArgumentNullException.ThrowIfNull(timeProvider);

        var materialisedPins = pins.ToList();
        var requiredRoles = Enum.GetValues<ContractPinRole>();
        var distinctRoles = materialisedPins
            .Select(pin => pin.Role)
            .Distinct()
            .ToList();

        var hasExactlySixPins = materialisedPins.Count == requiredRoles.Length;
        var hasDistinctRoles = distinctRoles.Count == requiredRoles.Length;
        var coversAllRoles = requiredRoles.All(role => distinctRoles.Contains(role));

        if (!hasExactlySixPins || !hasDistinctRoles || !coversAllRoles)
        {
            throw new ArgumentException(
                "Contract manifest must contain exactly one pin per contract role.",
                nameof(pins));
        }

        return new ContractManifest
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Version = version,
            CreatedBy = createdBy,
            CreatedAt = timeProvider.GetUtcNow(),
            _pins = materialisedPins
        };
    }
}