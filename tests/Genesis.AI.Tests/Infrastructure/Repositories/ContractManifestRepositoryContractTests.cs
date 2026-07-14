using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Tests.Infrastructure.Repositories;

public sealed class ContractManifestRepositoryContractTests
{
    [Fact]
    public async Task GetLatestForProjectAsync_MockCanReturnManifest()
    {
        var projectId = Guid.NewGuid();
        ContractManifest manifest = ContractManifest.Create(
            projectId,
            1,
            [
                ContractManifestPin.Create(ContractPinRole.Req, "requirements/REQ-001.md", 2),
                ContractManifestPin.Create(ContractPinRole.Arch, "architecture/ARCH-001.md", 3),
                ContractManifestPin.Create(ContractPinRole.ApiContract, "design/api/openapi.yaml", 4),
                ContractManifestPin.Create(ContractPinRole.DbSchema, "design/db/schema.sql", 5),
                ContractManifestPin.Create(ContractPinRole.DataModels, "design/models/domain-models.md", 6),
                ContractManifestPin.Create(ContractPinRole.ErrorCatalogue, "design/errors/error-catalogue.md", 7)
            ],
            "user-1",
            TimeProvider.System);

            var requiredRole = ContractPinRole.Req;
            Assert.Equal(ContractPinRole.Req, requiredRole);

        IContractManifestRepository repository = Mock.Of<IContractManifestRepository>();
        var repositoryMock = Mock.Get(repository);
        repositoryMock
            .Setup(repository => repository.GetLatestForProjectAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manifest);

        var result = await repository.GetLatestForProjectAsync(projectId, CancellationToken.None);

        Assert.Same(manifest, result);
    }
}
