using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace Genesis.AI.Tests.Services;

public class FoundationServiceTests
{
    private static readonly Guid ProjectId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly TimeProvider Time = TimeProvider.System;

    private readonly Mock<IArtefactRepository> _artefactRepositoryMock = new();
    private readonly Mock<IArtefactStorageService> _artefactStorageServiceMock = new();
    private readonly Mock<ILogger<FoundationService>> _loggerMock = new();

    private FoundationService CreateService() =>
        new(_artefactRepositoryMock.Object, _artefactStorageServiceMock.Object, _loggerMock.Object);

    // ── Constructor guards ──────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithNullArtefactRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new FoundationService(null!, _artefactStorageServiceMock.Object, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullArtefactStorageService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new FoundationService(_artefactRepositoryMock.Object, null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new FoundationService(_artefactRepositoryMock.Object, _artefactStorageServiceMock.Object, null!));
    }

    // ── Stages with no foundation prefixes (P1, P2) ────────────────────────

    [Theory]
    [InlineData(StageType.RequirementsDiscovery)]
    [InlineData(StageType.Prototype)]
    public async Task BuildFoundationContentAsync_WhenStageHasNoFoundationPrefixes_ReturnsEmptyString(StageType stageType)
    {
        var service = CreateService();

        var result = await service.BuildFoundationContentAsync(ProjectId, stageType, CancellationToken.None);

        Assert.Equal(string.Empty, result);
        _artefactRepositoryMock.Verify(
            repo => repo.GetProjectArtefactManifestAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── No matching artefacts in manifest ──────────────────────────────────

    [Fact]
    public async Task BuildFoundationContentAsync_WhenManifestHasNoMatchingArtefacts_ReturnsEmptyString()
    {
        _artefactRepositoryMock
            .Setup(repo => repo.GetProjectArtefactManifestAsync(ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artefact>().AsReadOnly());

        var service = CreateService();

        var result = await service.BuildFoundationContentAsync(ProjectId, StageType.Architecture, CancellationToken.None);

        Assert.Equal(string.Empty, result);
        _artefactStorageServiceMock.Verify(
            storage => storage.GetContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Happy path — artefacts loaded ──────────────────────────────────────

    [Fact]
    public async Task BuildFoundationContentAsync_WhenFoundationArtefactsExist_ReturnsFormattedContent()
    {
        var artefact = Artefact.CreateS3Artefact(
            projectId: ProjectId,
            version: 1,
            filePath: "requirements/REQ-001.md",
            s3Key: "projects/11111111-1111-1111-1111-111111111111/artefacts/requirements/REQ-001.md/v1",
            contentType: "text/markdown",
            sizeBytes: 100,
            createdBy: "test",
            timeProvider: Time);

        _artefactRepositoryMock
            .Setup(repo => repo.GetProjectArtefactManifestAsync(ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artefact> { artefact }.AsReadOnly());

        _artefactStorageServiceMock
            .Setup(storage => storage.GetContentAsync(artefact.S3Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync("# REQ-001 — User Login");

        var service = CreateService();

        var result = await service.BuildFoundationContentAsync(ProjectId, StageType.Architecture, CancellationToken.None);

        Assert.Contains("## PROJECT FOUNDATION", result);
        Assert.Contains("requirements/REQ-001.md (v1)", result);
        Assert.Contains("# REQ-001 — User Login", result);
    }

    [Fact]
    public async Task BuildFoundationContentAsync_WhenMultipleFoundationArtefactsExist_SortsAlphabeticallyByFilePath()
    {
        var artefactB = Artefact.CreateS3Artefact(ProjectId, 1, "requirements/REQ-002.md", "key-b", "text/markdown", 50, "test", Time);
        var artefactA = Artefact.CreateS3Artefact(ProjectId, 1, "requirements/REQ-001.md", "key-a", "text/markdown", 50, "test", Time);

        _artefactRepositoryMock
            .Setup(repo => repo.GetProjectArtefactManifestAsync(ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artefact> { artefactB, artefactA }.AsReadOnly());

        _artefactStorageServiceMock
            .Setup(storage => storage.GetContentAsync("key-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Content A");
        _artefactStorageServiceMock
            .Setup(storage => storage.GetContentAsync("key-b", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Content B");

        var service = CreateService();

        var result = await service.BuildFoundationContentAsync(ProjectId, StageType.Architecture, CancellationToken.None);

        var posA = result.IndexOf("REQ-001.md", StringComparison.Ordinal);
        var posB = result.IndexOf("REQ-002.md", StringComparison.Ordinal);
        Assert.True(posA < posB, "REQ-001.md should appear before REQ-002.md (alphabetical order)");
    }

    // ── Artefact not found in S3 (null content) ────────────────────────────

    [Fact]
    public async Task BuildFoundationContentAsync_WhenArtefactNotFoundInStorage_SkipsArtefactAndStillReturnsContent()
    {
        var missingArtefact = Artefact.CreateS3Artefact(ProjectId, 1, "requirements/REQ-001.md", "key-missing", "text/markdown", 50, "test", Time);
        var presentArtefact = Artefact.CreateS3Artefact(ProjectId, 1, "requirements/REQ-002.md", "key-present", "text/markdown", 50, "test", Time);

        _artefactRepositoryMock
            .Setup(repo => repo.GetProjectArtefactManifestAsync(ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artefact> { missingArtefact, presentArtefact }.AsReadOnly());

        _artefactStorageServiceMock
            .Setup(storage => storage.GetContentAsync("key-missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        _artefactStorageServiceMock
            .Setup(storage => storage.GetContentAsync("key-present", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Present content");

        var service = CreateService();

        var result = await service.BuildFoundationContentAsync(ProjectId, StageType.Architecture, CancellationToken.None);

        Assert.DoesNotContain("REQ-001.md", result);
        Assert.Contains("REQ-002.md", result);
        Assert.Contains("Present content", result);
    }

    [Fact]
    public async Task BuildFoundationContentAsync_WhenAllArtefactsNotFoundInStorage_ReturnsFoundationHeaderOnly()
    {
        var artefact = Artefact.CreateS3Artefact(ProjectId, 1, "requirements/REQ-001.md", "key-missing", "text/markdown", 50, "test", Time);

        _artefactRepositoryMock
            .Setup(repo => repo.GetProjectArtefactManifestAsync(ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artefact> { artefact }.AsReadOnly());

        _artefactStorageServiceMock
            .Setup(storage => storage.GetContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var service = CreateService();

        var result = await service.BuildFoundationContentAsync(ProjectId, StageType.Architecture, CancellationToken.None);

        // Header is written before loading — missing artefacts should leave us with just the header section
        Assert.Contains("## PROJECT FOUNDATION", result);
        Assert.DoesNotContain("REQ-001.md (v", result);
    }

    // ── Non-matching artefacts are excluded ────────────────────────────────

    [Fact]
    public async Task BuildFoundationContentAsync_WhenManifestContainsMixedArtefacts_IncludesOnlyFoundationArtefacts()
    {
        // Architecture stage: only "requirements/" prefix is in scope
        var reqArtefact = Artefact.CreateS3Artefact(ProjectId, 1, "requirements/REQ-001.md", "key-req", "text/markdown", 50, "test", Time);
        var archArtefact = Artefact.CreateS3Artefact(ProjectId, 1, "architecture/ARCH-001.md", "key-arch", "text/markdown", 50, "test", Time);

        _artefactRepositoryMock
            .Setup(repo => repo.GetProjectArtefactManifestAsync(ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artefact> { reqArtefact, archArtefact }.AsReadOnly());

        _artefactStorageServiceMock
            .Setup(storage => storage.GetContentAsync("key-req", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Requirements content");

        var service = CreateService();

        var result = await service.BuildFoundationContentAsync(ProjectId, StageType.Architecture, CancellationToken.None);

        Assert.Contains("requirements/REQ-001.md", result);
        Assert.DoesNotContain("architecture/ARCH-001.md", result);
        _artefactStorageServiceMock.Verify(
            storage => storage.GetContentAsync("key-arch", It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Cancellation ───────────────────────────────────────────────────────

    [Fact]
    public async Task BuildFoundationContentAsync_WhenCancelled_ThrowsOperationCancelledException()
    {
        var artefact = Artefact.CreateS3Artefact(ProjectId, 1, "requirements/REQ-001.md", "key-1", "text/markdown", 50, "test", Time);

        _artefactRepositoryMock
            .Setup(repo => repo.GetProjectArtefactManifestAsync(ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artefact> { artefact }.AsReadOnly());

        _artefactStorageServiceMock
            .Setup(storage => storage.GetContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var service = CreateService();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.BuildFoundationContentAsync(ProjectId, StageType.Architecture, new CancellationToken(true)));
    }
}
