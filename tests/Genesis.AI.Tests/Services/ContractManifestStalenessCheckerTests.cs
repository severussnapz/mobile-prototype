using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Services;

namespace Genesis.AI.Tests.Services;

public class ContractManifestStalenessCheckerTests
{
    private static readonly TimeProvider Time = TimeProvider.System;

    private readonly Mock<IArtefactRepository> _artefactRepositoryMock = new();

    private ContractManifestStalenessChecker CreateChecker()
    {
        return new ContractManifestStalenessChecker(_artefactRepositoryMock.Object);
    }

    [Fact]
    public async Task CheckStalenessAsync_WhenAllArtefactsCurrentAndCommentsParsed_ReturnsEmptyList()
    {
        var projectId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;
        const string manifestContent = """
            <!-- contract-manifest-version: 7 -->
            <!-- req-provenance: requirements/REQ-001.md@v6,requirements/REQ-002.md@v3 -->
            <!-- arch-provenance: architecture/ARCH.md@v4 -->
            """;

        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                projectId,
                "requirements/REQ-001.md",
                cancellationToken))
            .ReturnsAsync(CreateArtefact(projectId, "requirements/REQ-001.md", 6));
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                projectId,
                "requirements/REQ-002.md",
                cancellationToken))
            .ReturnsAsync(CreateArtefact(projectId, "requirements/REQ-002.md", 3));
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                projectId,
                "architecture/ARCH.md",
                cancellationToken))
            .ReturnsAsync(CreateArtefact(projectId, "architecture/ARCH.md", 4));

        var checker = CreateChecker();

        var result = await checker.CheckStalenessAsync(projectId, manifestContent, cancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task CheckStalenessAsync_WhenReqVersionDrifted_ReturnsWarningForThatReq()
    {
        var projectId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;
        const string manifestContent = """
            <!-- contract-manifest-version: 7 -->
            <!-- req-provenance: requirements/REQ-001.md@v6 -->
            <!-- arch-provenance: architecture/ARCH.md@v4 -->
            """;

        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                projectId,
                "requirements/REQ-001.md",
                cancellationToken))
            .ReturnsAsync(CreateArtefact(projectId, "requirements/REQ-001.md", 8));
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                projectId,
                "architecture/ARCH.md",
                cancellationToken))
            .ReturnsAsync(CreateArtefact(projectId, "architecture/ARCH.md", 4));

        var checker = CreateChecker();

        var result = await checker.CheckStalenessAsync(projectId, manifestContent, cancellationToken);

        Assert.Single(result);
        Assert.Contains("REQ-001.md", result[0], StringComparison.Ordinal);
        Assert.Contains("v6", result[0], StringComparison.Ordinal);
        Assert.Contains("v8", result[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task CheckStalenessAsync_WhenArchVersionDrifted_ReturnsWarningForArch()
    {
        var projectId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;
        const string manifestContent = """
            <!-- contract-manifest-version: 7 -->
            <!-- req-provenance: requirements/REQ-001.md@v6 -->
            <!-- arch-provenance: architecture/ARCH.md@v4 -->
            """;

        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                projectId,
                "requirements/REQ-001.md",
                cancellationToken))
            .ReturnsAsync(CreateArtefact(projectId, "requirements/REQ-001.md", 6));
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                projectId,
                "architecture/ARCH.md",
                cancellationToken))
            .ReturnsAsync(CreateArtefact(projectId, "architecture/ARCH.md", 5));

        var checker = CreateChecker();

        var result = await checker.CheckStalenessAsync(projectId, manifestContent, cancellationToken);

        Assert.Single(result);
        Assert.Contains("ARCH.md", result[0], StringComparison.Ordinal);
        Assert.Contains("v4", result[0], StringComparison.Ordinal);
        Assert.Contains("v5", result[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task CheckStalenessAsync_WhenReqArtefactMissing_ReturnsWarningForMissingReq()
    {
        var projectId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;
        const string manifestContent = """
            <!-- contract-manifest-version: 7 -->
            <!-- req-provenance: requirements/REQ-001.md@v6 -->
            <!-- arch-provenance: architecture/ARCH.md@v4 -->
            """;

        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                projectId,
                "requirements/REQ-001.md",
                cancellationToken))
            .ReturnsAsync((Artefact?)null);
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                projectId,
                "architecture/ARCH.md",
                cancellationToken))
            .ReturnsAsync(CreateArtefact(projectId, "architecture/ARCH.md", 4));

        var checker = CreateChecker();

        var result = await checker.CheckStalenessAsync(projectId, manifestContent, cancellationToken);

        Assert.Single(result);
        Assert.Contains("requirements/REQ-001.md", result[0], StringComparison.Ordinal);
        Assert.Contains("missing", result[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckStalenessAsync_WhenMultipleReqsOneStale_ReturnsOneWarning()
    {
        var projectId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;
        const string manifestContent = """
            <!-- contract-manifest-version: 7 -->
            <!-- req-provenance: requirements/REQ-001.md@v6,requirements/REQ-002.md@v3 -->
            <!-- arch-provenance: architecture/ARCH.md@v4 -->
            """;

        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                projectId,
                "requirements/REQ-001.md",
                cancellationToken))
            .ReturnsAsync(CreateArtefact(projectId, "requirements/REQ-001.md", 6));
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                projectId,
                "requirements/REQ-002.md",
                cancellationToken))
            .ReturnsAsync(CreateArtefact(projectId, "requirements/REQ-002.md", 5));
        _artefactRepositoryMock
            .Setup(repository => repository.GetByProjectAndFilePathAsync(
                projectId,
                "architecture/ARCH.md",
                cancellationToken))
            .ReturnsAsync(CreateArtefact(projectId, "architecture/ARCH.md", 4));

        var checker = CreateChecker();

        var result = await checker.CheckStalenessAsync(projectId, manifestContent, cancellationToken);

        Assert.Single(result);
        Assert.Contains("REQ-002", result[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task CheckStalenessAsync_WhenNoHtmlCommentsPresent_ReturnsEmptyList()
    {
        var projectId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;
        const string manifestContent = "# Contract Manifest\n\n## 1. Status Header\nManifest version: 7";

        var checker = CreateChecker();

        var result = await checker.CheckStalenessAsync(projectId, manifestContent, cancellationToken);

        Assert.Empty(result);
        _artefactRepositoryMock.Verify(
            repository => repository.GetByProjectAndFilePathAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CheckStalenessAsync_WhenManifestContentEmpty_ReturnsEmptyList()
    {
        var projectId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        var checker = CreateChecker();

        var result = await checker.CheckStalenessAsync(projectId, string.Empty, cancellationToken);

        Assert.Empty(result);
        _artefactRepositoryMock.Verify(
            repository => repository.GetByProjectAndFilePathAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static Artefact CreateArtefact(Guid projectId, string filePath, int version)
    {
        return Artefact.CreateS3Artefact(
            projectId,
            version,
            filePath,
            $"projects/test/artefacts/{filePath}/v{version}",
            "text/markdown",
            64,
            "test",
            Time,
            true);
    }
}