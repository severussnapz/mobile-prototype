using System.Text;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.GitHub;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Services.GitHub;
using Moq;
using Microsoft.Extensions.Logging;

namespace Genesis.AI.Tests.Infrastructure.Services.GitHub;

public sealed class GitHubArtefactPushServiceTests
{
    private readonly Mock<IProjectRepository> _projectRepository;
    private readonly Mock<IArtefactStorageService> _artefactStorageService;
    private readonly Mock<IGitHubTokenService> _tokenService;
    private readonly Mock<IGitHubContentsService> _contentsService;
    private readonly Mock<IPushFailureLogRepository> _pushFailureLogRepository;
    private readonly Mock<IAssemblyVersionProvider> _versionProvider;
    private readonly TimeProvider _timeProvider;
    private readonly Mock<ILogger<GitHubArtefactPushService>> _logger;
    private readonly GitHubArtefactPushService _service;

    public GitHubArtefactPushServiceTests()
    {
        _projectRepository = new Mock<IProjectRepository>();
        _artefactStorageService = new Mock<IArtefactStorageService>();
        _tokenService = new Mock<IGitHubTokenService>();
        _contentsService = new Mock<IGitHubContentsService>();
        _pushFailureLogRepository = new Mock<IPushFailureLogRepository>();
        _versionProvider = new Mock<IAssemblyVersionProvider>();
        _timeProvider = TimeProvider.System;
        _logger = new Mock<ILogger<GitHubArtefactPushService>>();

        _tokenService
            .Setup(service => service.GetInstallationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("token-abc");
        _contentsService
            .Setup(service => service.FileExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _contentsService
            .Setup(service => service.GetFileShaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("abc123");
        _contentsService
            .Setup(service => service.PushFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GitHubPushResult("sha123", "https://github.com/emisgroup/emis-x-docs/blob/main/.gitkeep"));
        _versionProvider.Setup(provider => provider.GetVersion()).Returns("1.0.0.0");
        _artefactStorageService
            .Setup(service => service.GetContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("# REQ-001\nContent");
        _artefactStorageService
            .Setup(service => service.GetBinaryContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 1, 2, 3 });

        _service = new GitHubArtefactPushService(
            _projectRepository.Object,
            _artefactStorageService.Object,
            _tokenService.Object,
            _contentsService.Object,
            _pushFailureLogRepository.Object,
            _versionProvider.Object,
            _timeProvider,
            _logger.Object);
    }

    [Fact]
    public async Task PushAsync_NoGitHubConfig_ReturnsWithoutPushing()
    {
        var project = new Project(
            "TST", "Test Project", "desc", "PORTASK0001045",
            ComplianceDomain.Generic, "creator", _timeProvider);

        _projectRepository
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        await _service.PushAsync(
            project.Id, Guid.NewGuid(), "requirements/REQ-001.md", 1,
            "text/markdown", "s3key", "user@emisgroup.com", CancellationToken.None);

        _contentsService.Verify(service => service.PushFileAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        _pushFailureLogRepository.Verify(repository => repository.AddAsync(
            It.IsAny<Genesis.AI.Domain.AggregatesModel.PushFailureLogAggregate.PushFailureLog>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PushAsync_UnmappedPath_SkipsWithoutPushing()
    {
        var project = CreateProjectWithGitHub();
        _projectRepository
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        await _service.PushAsync(
            project.Id, Guid.NewGuid(), "unknown/SOMETHING.md", 1,
            "text/markdown", "s3key", "user@emisgroup.com", CancellationToken.None);

        _contentsService.Verify(service => service.PushFileAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        _pushFailureLogRepository.Verify(repository => repository.AddAsync(
            It.IsAny<Genesis.AI.Domain.AggregatesModel.PushFailureLogAggregate.PushFailureLog>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PushAsync_MarkdownFile_ReadsTextContent_PushesCorrectPath()
    {
        var project = CreateProjectWithGitHub();
        var artefactId = Guid.NewGuid();
        var s3Key = "projects/{id}/artefacts/requirements/REQ-001.md/v1";

        _projectRepository
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        await _service.PushAsync(
            project.Id, artefactId, "requirements/REQ-001.md", 1,
            "text/markdown", s3Key, "user@emisgroup.com", CancellationToken.None);

        _artefactStorageService.Verify(service => service.GetContentAsync(s3Key, It.IsAny<CancellationToken>()), Times.Once);

        var callArgs = _contentsService.Invocations
            .Where(c => c.Method.Name == nameof(IGitHubContentsService.PushFileAsync))
            .First();
        var path = (string)callArgs.Arguments[3]!;
        Assert.Equal(".genesis/requirements/REQ-001.md", path);

        var content = (byte[])callArgs.Arguments[4]!;
        Assert.Equal("# REQ-001\nContent", Encoding.UTF8.GetString(content));
    }

    [Fact]
    public async Task PushAsync_BinaryFile_ReadsBinaryContent_PushesCorrectPath()
    {
        var project = CreateProjectWithGitHub();
        var artefactId = Guid.NewGuid();
        var s3Key = "projects/{id}/artefacts/clinical-safety/DCB0129-001.xlsx/v1";

        _projectRepository
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        await _service.PushAsync(
            project.Id, artefactId, "clinical-safety/DCB0129-001.xlsx", 1,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            s3Key, "user@emisgroup.com", CancellationToken.None);

        _artefactStorageService.Verify(service => service.GetBinaryContentAsync(s3Key, It.IsAny<CancellationToken>()), Times.Once);

        var callArgs = _contentsService.Invocations
            .Where(c => c.Method.Name == nameof(IGitHubContentsService.PushFileAsync))
            .First();
        var path = (string)callArgs.Arguments[3]!;
        Assert.Equal(".genesis/clinical-safety/DCB0129-001.xlsx", path);
    }

    [Fact]
    public async Task PushAsync_CommitMessageContainsAllTrailers()
    {
        var project = CreateProjectWithGitHub();
        var artefactId = Guid.NewGuid();

        _projectRepository
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        await _service.PushAsync(
            project.Id, artefactId, "requirements/REQ-001.md", 3,
            "text/markdown", "s3key", "user@emisgroup.com", CancellationToken.None);

        var callArgs = _contentsService.Invocations
            .Where(c => c.Method.Name == nameof(IGitHubContentsService.PushFileAsync))
            .First();
        var commitMessage = (string)callArgs.Arguments[5]!;

        Assert.Contains("Triggered-By: user@emisgroup.com", commitMessage);
        Assert.Contains("Approved-By: user@emisgroup.com", commitMessage);
        Assert.Contains("Project-ID:", commitMessage);
        Assert.Contains("Artefact-ID:", commitMessage);
        Assert.Contains("Genesis-AI-Version: 1.0.0.0", commitMessage);
    }

    [Fact]
    public async Task PushAsync_ExistingFileSha_ResolvedBeforePush()
    {
        var project = CreateProjectWithGitHub();
        var artefactId = Guid.NewGuid();

        _projectRepository
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        await _service.PushAsync(
            project.Id, artefactId, "requirements/REQ-001.md", 1,
            "text/markdown", "s3key", "user@emisgroup.com", CancellationToken.None);

        var callArgs = _contentsService.Invocations
            .Where(c => c.Method.Name == nameof(IGitHubContentsService.PushFileAsync))
            .First();
        var existingSha = (string?)callArgs.Arguments[6];
        Assert.Equal("abc123", existingSha);
    }

    [Fact]
    public async Task PushAsync_PushThrows_WritesToPushFailureLog()
    {
        var project = CreateProjectWithGitHub();
        var artefactId = Guid.NewGuid();
        var filePath = "requirements/REQ-001.md";

        _projectRepository
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _contentsService
            .Setup(service => service.PushFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("push failed"));

        await _service.PushAsync(
            project.Id, artefactId, filePath, 1,
            "text/markdown", "s3key", "user@emisgroup.com", CancellationToken.None);

        _pushFailureLogRepository.Verify(repository => repository.AddAsync(It.Is<Genesis.AI.Domain.AggregatesModel.PushFailureLogAggregate.PushFailureLog>(
                log => log.FilePath == filePath &&
                       log.ErrorMessage.Contains("push failed") &&
                       log.RetryCount == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PushAsync_PushThrows_DoesNotThrowToCaller()
    {
        var project = CreateProjectWithGitHub();
        var artefactId = Guid.NewGuid();

        _projectRepository
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _contentsService
            .Setup(service => service.PushFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("push failed"));

        var exception = await Record.ExceptionAsync(() =>
            _service.PushAsync(project.Id, artefactId, "requirements/REQ-001.md", 1,
                "text/markdown", "s3key", "user@emisgroup.com", CancellationToken.None));

        Assert.Null(exception);
    }

    [Fact]
    public async Task PushAsync_S3ReadReturnsNull_WritesToPushFailureLog()
    {
        var project = CreateProjectWithGitHub();
        var artefactId = Guid.NewGuid();
        var filePath = "requirements/REQ-001.md";

        _projectRepository
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _artefactStorageService
            .Setup(service => service.GetContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        await _service.PushAsync(
            project.Id, artefactId, filePath, 1,
            "text/markdown", "s3key", "user@emisgroup.com", CancellationToken.None);

        _pushFailureLogRepository.Verify(repository => repository.AddAsync(It.Is<Genesis.AI.Domain.AggregatesModel.PushFailureLogAggregate.PushFailureLog>(
                log => log.FilePath == filePath),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Project CreateProjectWithGitHub()
    {
        var project = new Project(
            "TST", "Test Project", "A test project", "PORTASK0001045",
            ComplianceDomain.ClinicalUk, "creator", TimeProvider.System);

        project.SetGitHubConfig(
            "https://github.com/emisgroup/emis-x-docs",
            "https://github.com/emisgroup/emis-x-docs-app",
            "emisgroup",
            "emis-x-docs",
            "144995615",
            TimeProvider.System);

        return project;
    }
}
