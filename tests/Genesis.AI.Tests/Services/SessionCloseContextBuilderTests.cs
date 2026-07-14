using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Services;

namespace Genesis.AI.Tests.Services;

public class SessionCloseContextBuilderTests
{
    private static readonly TimeProvider Time = TimeProvider.System;

    private readonly Mock<IArtefactRepository> _artefactRepositoryMock = new();
    private readonly Mock<IArtefactStorageService> _artefactStorageServiceMock = new();

    private SessionCloseContextBuilder CreateBuilder()
    {
        return new SessionCloseContextBuilder(
            _artefactRepositoryMock.Object,
            _artefactStorageServiceMock.Object);
    }

    [Fact]
    public async Task BuildSessionCloseContextAsync_WhenPublishedSessionCloseArtefactExists_ReturnsFormattedBlockContainingContent()
    {
        var projectId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;
        var artefact = Artefact.CreateS3Artefact(
            projectId,
            1,
            "session-close/SESSION-CLOSE-P01.md",
            "projects/test/artefacts/session-close/SESSION-CLOSE-P01.md/v1",
            "text/markdown",
            64,
            "test",
            Time,
            true);

        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                projectId,
                "session-close/SESSION-CLOSE-P01.md",
                cancellationToken))
            .ReturnsAsync(artefact);

        _artefactStorageServiceMock
            .Setup(storage => storage.GetContentAsync(artefact.S3Key, cancellationToken))
            .ReturnsAsync("## Session Close\nResume point: finish NFRs");

        var builder = CreateBuilder();

        var result = await builder.BuildSessionCloseContextAsync(
            projectId,
            StageType.RequirementsDiscovery,
            cancellationToken);

        Assert.NotEqual(string.Empty, result);
        Assert.Contains("Resume point: finish NFRs", result, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BuildSessionCloseContextAsync_WhenNoArtefactExists_ReturnsEmptyString()
    {
        var projectId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                projectId,
                "session-close/SESSION-CLOSE-P01.md",
                cancellationToken))
            .ReturnsAsync((Artefact?)null);

        var builder = CreateBuilder();

        var result = await builder.BuildSessionCloseContextAsync(
            projectId,
            StageType.RequirementsDiscovery,
            cancellationToken);

        Assert.Equal(string.Empty, result);
        _artefactStorageServiceMock.Verify(
            storage => storage.GetContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(StageType.Design, "session-close/SESSION-CLOSE-P04.md")]
    [InlineData(StageType.ClinicalSafety, "session-close/SESSION-CLOSE-P06.md")]
    public async Task BuildSessionCloseContextAsync_ResolvesCorrectFilePathPerStage(StageType stageType, string expectedFilePath)
    {
        var projectId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                projectId,
                expectedFilePath,
                cancellationToken))
            .ReturnsAsync((Artefact?)null);

        var builder = CreateBuilder();

        _ = await builder.BuildSessionCloseContextAsync(projectId, stageType, cancellationToken);

        _artefactRepositoryMock.Verify(
            repository => repository.GetByProjectAndFilePathAsync(projectId, expectedFilePath, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task BuildSessionCloseContextAsync_UnsupportedStage_ReturnsEmptyString()
    {
        var projectId = Guid.NewGuid();
        var builder = CreateBuilder();

        var result = await builder.BuildSessionCloseContextAsync(
            projectId,
            StageType.Normalisation,
            CancellationToken.None);

        Assert.Equal(string.Empty, result);
    }
}