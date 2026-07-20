using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Services;

namespace Genesis.AI.Tests.Services;

public class ContractManifestContextBuilderTests
{
    private const string ManifestFilePath = "design/CONTRACT-MANIFEST.md";

    private static readonly TimeProvider Time = TimeProvider.System;

    private readonly Mock<IArtefactRepository> _artefactRepositoryMock = new();
    private readonly Mock<IArtefactStorageService> _artefactStorageServiceMock = new();

    private ContractManifestContextBuilder CreateBuilder()
    {
        return new ContractManifestContextBuilder(
            _artefactRepositoryMock.Object,
            _artefactStorageServiceMock.Object);
    }

    [Fact]
    public async Task BuildContractManifestContextAsync_WhenArtefactExists_ReturnsBlockContainingContent()
    {
        var projectId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;
        var artefact = Artefact.CreateS3Artefact(
            projectId,
            1,
            ManifestFilePath,
            "projects/test/artefacts/design/CONTRACT-MANIFEST.md/v1",
            "text/markdown",
            128,
            "test",
            Time,
            true);

        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                projectId,
                ManifestFilePath,
                cancellationToken))
            .ReturnsAsync(artefact);

        _artefactStorageServiceMock
            .Setup(storage => storage.GetContentAsync(artefact.S3Key, cancellationToken))
            .ReturnsAsync("## 1. Status Header\nManifest version: 3\nContract complete: YES");

        var builder = CreateBuilder();

        var result = await builder.BuildContractManifestContextAsync(
            projectId,
            StageType.Design,
            cancellationToken);

        Assert.NotEqual(string.Empty, result);
        Assert.Contains("## Contract Manifest", result, StringComparison.Ordinal);
        Assert.Contains("Manifest version: 3", result, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BuildContractManifestContextAsync_WhenNoArtefactExists_ReturnsEmptyString()
    {
        var projectId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                projectId,
                ManifestFilePath,
                cancellationToken))
            .ReturnsAsync((Artefact?)null);

        var builder = CreateBuilder();

        var result = await builder.BuildContractManifestContextAsync(
            projectId,
            StageType.Design,
            cancellationToken);

        Assert.Equal(string.Empty, result);
        _artefactStorageServiceMock.Verify(
            storage => storage.GetContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(StageType.Design)]
    [InlineData(StageType.ClinicalSafety)]
    [InlineData(StageType.InformationGovernance)]
    [InlineData(StageType.Security)]
    public async Task BuildContractManifestContextAsync_ConsumingStage_QueriesManifestFilePath(StageType stageType)
    {
        var projectId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                projectId,
                ManifestFilePath,
                cancellationToken))
            .ReturnsAsync((Artefact?)null);

        var builder = CreateBuilder();

        _ = await builder.BuildContractManifestContextAsync(projectId, stageType, cancellationToken);

        _artefactRepositoryMock.Verify(
            repository => repository.GetByProjectAndFilePathAsync(projectId, ManifestFilePath, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task BuildContractManifestContextAsync_NonConsumingStage_ReturnsEmptyStringAndRepositoryNeverCalled()
    {
        var projectId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;
        var builder = CreateBuilder();

        var result = await builder.BuildContractManifestContextAsync(
            projectId,
            StageType.Normalisation,
            cancellationToken);

        Assert.Equal(string.Empty, result);
        _artefactRepositoryMock.Verify(
            repository => repository.GetByProjectAndFilePathAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
