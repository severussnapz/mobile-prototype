using Genesis.AI.Domain.Enums;
using Microsoft.Extensions.Time.Testing;

namespace Genesis.AI.Tests.Domain;

public sealed class ContractManifestTests
{
    [Fact]
    public void Create_WithAllSixRolesValid_CreatesManifestWithSixPins()
    {
        var projectId = Guid.NewGuid();
        var pins = CreateAllPins();

        ContractManifest manifest = ContractManifest.Create(
            projectId,
            1,
            pins,
            "user-1",
            TimeProvider.System);

        Assert.NotEqual(Guid.Empty, manifest.Id);
        Assert.Equal(projectId, manifest.ProjectId);
        Assert.Equal(1, manifest.Version);
        Assert.Equal("user-1", manifest.CreatedBy);
        Assert.Equal(6, manifest.Pins.Count);
    }

    [Fact]
    public void Create_MissingARole_ThrowsArgumentException()
    {
        var pins = new List<ContractManifestPin>
        {
            ContractManifestPin.Create(ContractPinRole.Req, "requirements/REQ-001.md", 2),
            ContractManifestPin.Create(ContractPinRole.Arch, "architecture/ARCH-001.md", 3),
            ContractManifestPin.Create(ContractPinRole.ApiContract, "design/api/openapi.yaml", 4),
            ContractManifestPin.Create(ContractPinRole.DbSchema, "design/db/schema.sql", 5),
            ContractManifestPin.Create(ContractPinRole.DataModels, "design/models/domain-models.md", 6)
        };

        Assert.Throws<ArgumentException>(() => ContractManifest.Create(
            Guid.NewGuid(),
            1,
            pins,
            "user-1",
            TimeProvider.System));
    }

    [Fact]
    public void Create_DuplicateRole_ThrowsArgumentException()
    {
        var pins = new List<ContractManifestPin>
        {
            ContractManifestPin.Create(ContractPinRole.Req, "requirements/REQ-001.md", 2),
            ContractManifestPin.Create(ContractPinRole.Req, "requirements/REQ-002.md", 3),
            ContractManifestPin.Create(ContractPinRole.Arch, "architecture/ARCH-001.md", 4),
            ContractManifestPin.Create(ContractPinRole.ApiContract, "design/api/openapi.yaml", 5),
            ContractManifestPin.Create(ContractPinRole.DbSchema, "design/db/schema.sql", 6),
            ContractManifestPin.Create(ContractPinRole.DataModels, "design/models/domain-models.md", 7),
            ContractManifestPin.Create(ContractPinRole.ErrorCatalogue, "design/errors/error-catalogue.md", 8)
        };

        Assert.Throws<ArgumentException>(() => ContractManifest.Create(
            Guid.NewGuid(),
            1,
            pins,
            "user-1",
            TimeProvider.System));
    }

    [Fact]
    public void Create_PinWithEmptyFilePath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ContractManifestPin.Create(
            ContractPinRole.Req,
            " ",
            1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_PinWithZeroOrNegativeVersion_ThrowsArgumentException(int pinnedVersion)
    {
        Assert.Throws<ArgumentException>(() => ContractManifestPin.Create(
            ContractPinRole.Req,
            "requirements/REQ-001.md",
            pinnedVersion));
    }

    [Fact]
    public void Create_SetsCreatedAtFromTimeProvider()
    {
        var fixedNow = new DateTimeOffset(2026, 7, 10, 12, 34, 56, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(fixedNow);

        ContractManifest manifest = ContractManifest.Create(
            Guid.NewGuid(),
            1,
            CreateAllPins(),
            "user-1",
            timeProvider);

        Assert.Equal(fixedNow, manifest.CreatedAt);
    }

    [Fact]
    public void Create_ExposesPinsAsReadOnly()
    {
        var pins = CreateAllPins();
        ContractManifest manifest = ContractManifest.Create(
            Guid.NewGuid(),
            1,
            pins,
            "user-1",
            TimeProvider.System);

        var pinByRole = manifest.Pins.ToDictionary(pin => pin.Role);

        Assert.Equal(6, manifest.Pins.Count);
        Assert.Equal("requirements/REQ-001.md", pinByRole[ContractPinRole.Req].FilePath);
        Assert.Equal(2, pinByRole[ContractPinRole.Req].PinnedVersion);
        Assert.Equal("architecture/ARCH-001.md", pinByRole[ContractPinRole.Arch].FilePath);
        Assert.Equal(3, pinByRole[ContractPinRole.Arch].PinnedVersion);
        Assert.Equal("design/api/openapi.yaml", pinByRole[ContractPinRole.ApiContract].FilePath);
        Assert.Equal(4, pinByRole[ContractPinRole.ApiContract].PinnedVersion);
        Assert.Equal("design/db/schema.sql", pinByRole[ContractPinRole.DbSchema].FilePath);
        Assert.Equal(5, pinByRole[ContractPinRole.DbSchema].PinnedVersion);
        Assert.Equal("design/models/domain-models.md", pinByRole[ContractPinRole.DataModels].FilePath);
        Assert.Equal(6, pinByRole[ContractPinRole.DataModels].PinnedVersion);
        Assert.Equal("design/errors/error-catalogue.md", pinByRole[ContractPinRole.ErrorCatalogue].FilePath);
        Assert.Equal(7, pinByRole[ContractPinRole.ErrorCatalogue].PinnedVersion);
    }

    private static IReadOnlyList<ContractManifestPin> CreateAllPins()
    {
        return
        [
            ContractManifestPin.Create(ContractPinRole.Req, "requirements/REQ-001.md", 2),
            ContractManifestPin.Create(ContractPinRole.Arch, "architecture/ARCH-001.md", 3),
            ContractManifestPin.Create(ContractPinRole.ApiContract, "design/api/openapi.yaml", 4),
            ContractManifestPin.Create(ContractPinRole.DbSchema, "design/db/schema.sql", 5),
            ContractManifestPin.Create(ContractPinRole.DataModels, "design/models/domain-models.md", 6),
            ContractManifestPin.Create(ContractPinRole.ErrorCatalogue, "design/errors/error-catalogue.md", 7)
        ];
    }
}
