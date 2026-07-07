using System.Text;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.GitHub;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Services.GitHub;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Genesis.AI.Tests.Infrastructure.Services.GitHub;

public sealed class GitHubArtefactPushServiceTests
{
    private readonly IProjectRepository _projectRepository;
    private readonly IArtefactStorageService _artefactStorageService;
    private readonly IGitHubTokenService _tokenService;
    private readonly IGitHubContentsService _contentsService;
    private readonly IPushFailureLogRepository _pushFailureLogRepository;
    private readonly IAssemblyVersionProvider _versionProvider;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<GitHubArtefactPushService> _logger;
    private readonly GitHubArtefactPushService _service;

    public GitHubArtefactPushServiceTests()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _artefactStorageService = Substitute.For<IArtefactStorageService>();
        _tokenService = Substitute.For<IGitHubTokenService>();
        _contentsService = Substitute.For<IGitHubContentsService>();
        _pushFailureLogRepository = Substitute.For<IPushFailureLogRepository>();
        _versionProvider = Substitute.For<IAssemblyVersionProvider>();
        _timeProvider = TimeProvider.System;
        _logger = Substitute.For<ILogger<GitHubArtefactPushService>>();

        _tokenService
            .GetInstallationTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("token-abc");
        _contentsService
            .FileExistsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _contentsService
            .GetFileShaAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("abc123");
        _contentsService
            .PushFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new GitHubPushResult("sha123", "https://github.com/emisgroup/emis-x-docs/blob/main/.gitkeep"));
        _versionProvider.GetVersion().Returns("1.0.0.0");
        _artefactStorageService
            .GetContentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("# REQ-001\nContent");
        _artefactStorageService
            .GetBinaryContentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 1, 2, 3 });

        _service = new GitHubArtefactPushService(
            _projectRepository,
            _artefactStorageService,
            _tokenService,
            _contentsService,
            _pushFailureLogRepository,
            _versionProvider,
            _timeProvider,
            _logger);
    }

    [Fact]
    public async Task PushAsync_NoGitHubConfig_ReturnsWithoutPushing()
    {
        var project = new Project(
            "TST", "Test Project", "desc", "PORTASK0001045",
            ComplianceDomain.Generic, "creator", _timeProvider);

        _projectRepository
            .GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        await _service.PushAsync(
            project.Id, Guid.NewGuid(), "requirements/REQ-001.md", 1,
            "text/markdown", "s3key", "user@emisgroup.com", CancellationToken.None);

        await _contentsService.DidNotReceive().PushFileAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await _pushFailureLogRepository.DidNotReceive().AddAsync(
            Arg.Any<Genesis.AI.Domain.AggregatesModel.PushFailureLogAggregate.PushFailureLog>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PushAsync_UnmappedPath_SkipsWithoutPushing()
    {
        var project = CreateProjectWithGitHub();
        _projectRepository
            .GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        await _service.PushAsync(
            project.Id, Guid.NewGuid(), "unknown/SOMETHING.md", 1,
            "text/markdown", "s3key", "user@emisgroup.com", CancellationToken.None);

        await _contentsService.DidNotReceive().PushFileAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await _pushFailureLogRepository.DidNotReceive().AddAsync(
            Arg.Any<Genesis.AI.Domain.AggregatesModel.PushFailureLogAggregate.PushFailureLog>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PushAsync_MarkdownFile_ReadsTextContent_PushesCorrectPath()
    {
        var project = CreateProjectWithGitHub();
        var artefactId = Guid.NewGuid();
        var s3Key = "projects/{id}/artefacts/requirements/REQ-001.md/v1";

        _projectRepository
            .GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        await _service.PushAsync(
            project.Id, artefactId, "requirements/REQ-001.md", 1,
            "text/markdown", s3Key, "user@emisgroup.com", CancellationToken.None);

        await _artefactStorageService.Received(1)
            .GetContentAsync(s3Key, Arg.Any<CancellationToken>());

        var callArgs = _contentsService.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IGitHubContentsService.PushFileAsync))
            .First();
        var path = (string)callArgs.GetArguments()[3]!;
        Assert.Equal(".genesis/requirements/REQ-001.md", path);

        var content = (byte[])callArgs.GetArguments()[4]!;
        Assert.Equal("# REQ-001\nContent", Encoding.UTF8.GetString(content));
    }

    [Fact]
    public async Task PushAsync_BinaryFile_ReadsBinaryContent_PushesCorrectPath()
    {
        var project = CreateProjectWithGitHub();
        var artefactId = Guid.NewGuid();
        var s3Key = "projects/{id}/artefacts/clinical-safety/DCB0129-001.xlsx/v1";

        _projectRepository
            .GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        await _service.PushAsync(
            project.Id, artefactId, "clinical-safety/DCB0129-001.xlsx", 1,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            s3Key, "user@emisgroup.com", CancellationToken.None);

        await _artefactStorageService.Received(1)
            .GetBinaryContentAsync(s3Key, Arg.Any<CancellationToken>());

        var callArgs = _contentsService.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IGitHubContentsService.PushFileAsync))
            .First();
        var path = (string)callArgs.GetArguments()[3]!;
        Assert.Equal(".genesis/clinical-safety/DCB0129-001.xlsx", path);
    }

    [Fact]
    public async Task PushAsync_CommitMessageContainsAllTrailers()
    {
        var project = CreateProjectWithGitHub();
        var artefactId = Guid.NewGuid();

        _projectRepository
            .GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        await _service.PushAsync(
            project.Id, artefactId, "requirements/REQ-001.md", 3,
            "text/markdown", "s3key", "user@emisgroup.com", CancellationToken.None);

        var callArgs = _contentsService.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IGitHubContentsService.PushFileAsync))
            .First();
        var commitMessage = (string)callArgs.GetArguments()[5]!;

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
            .GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        await _service.PushAsync(
            project.Id, artefactId, "requirements/REQ-001.md", 1,
            "text/markdown", "s3key", "user@emisgroup.com", CancellationToken.None);

        var callArgs = _contentsService.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IGitHubContentsService.PushFileAsync))
            .First();
        var existingSha = (string?)callArgs.GetArguments()[6];
        Assert.Equal("abc123", existingSha);
    }

    [Fact]
    public async Task PushAsync_PushThrows_WritesToPushFailureLog()
    {
        var project = CreateProjectWithGitHub();
        var artefactId = Guid.NewGuid();
        var filePath = "requirements/REQ-001.md";

        _projectRepository
            .GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);
        _contentsService
            .PushFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("push failed"));

        await _service.PushAsync(
            project.Id, artefactId, filePath, 1,
            "text/markdown", "s3key", "user@emisgroup.com", CancellationToken.None);

        await _pushFailureLogRepository.Received(1)
            .AddAsync(Arg.Is<Genesis.AI.Domain.AggregatesModel.PushFailureLogAggregate.PushFailureLog>(
                log => log.FilePath == filePath &&
                       log.ErrorMessage.Contains("push failed") &&
                       log.RetryCount == 0),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PushAsync_PushThrows_DoesNotThrowToCaller()
    {
        var project = CreateProjectWithGitHub();
        var artefactId = Guid.NewGuid();

        _projectRepository
            .GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);
        _contentsService
            .PushFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("push failed"));

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
            .GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);
        _artefactStorageService
            .GetContentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);

        await _service.PushAsync(
            project.Id, artefactId, filePath, 1,
            "text/markdown", "s3key", "user@emisgroup.com", CancellationToken.None);

        await _pushFailureLogRepository.Received(1)
            .AddAsync(Arg.Is<Genesis.AI.Domain.AggregatesModel.PushFailureLogAggregate.PushFailureLog>(
                log => log.FilePath == filePath),
                Arg.Any<CancellationToken>());
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
