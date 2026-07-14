using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.GitHub;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Services.GitHub;
using Moq;
using Microsoft.Extensions.Logging;

namespace Genesis.AI.Tests.Infrastructure.Services.GitHub;

public sealed class GenesisStructureScaffolderTests
{
    private readonly Mock<IProjectRepository> _projectRepository;
    private readonly Mock<IGitHubTokenService> _tokenService;
    private readonly Mock<IGitHubContentsService> _contentsService;
    private readonly Mock<IPushFailureLogRepository> _pushFailureLogRepository;
    private readonly Mock<ICodeownersGenerator> _codeownersGenerator;
    private readonly Mock<IProjectMarkdownGenerator> _markdownGenerator;
    private readonly Mock<IAssemblyVersionProvider> _versionProvider;
    private readonly Mock<ILogger<GenesisStructureScaffolder>> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly GenesisStructureScaffolder _scaffolder;

    public GenesisStructureScaffolderTests()
    {
        _projectRepository = new Mock<IProjectRepository>();
        _tokenService = new Mock<IGitHubTokenService>();
        _contentsService = new Mock<IGitHubContentsService>();
        _pushFailureLogRepository = new Mock<IPushFailureLogRepository>();
        _codeownersGenerator = new Mock<ICodeownersGenerator>();
        _markdownGenerator = new Mock<IProjectMarkdownGenerator>();
        _versionProvider = new Mock<IAssemblyVersionProvider>();
        _logger = new Mock<ILogger<GenesisStructureScaffolder>>();
        _timeProvider = TimeProvider.System;

        _tokenService
            .Setup(service => service.GetInstallationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("token-abc");
        _versionProvider.Setup(provider => provider.GetVersion()).Returns("1.0.0.0");
        _codeownersGenerator.Setup(generator => generator.Generate()).Returns("# CODEOWNERS");
        _markdownGenerator.Setup(generator => generator.Generate(It.IsAny<Project>())).Returns("# PROJECT");
        _contentsService
            .Setup(service => service.PushFileAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GitHubPushResult("sha123", "https://github.com/emisgroup/emis-x-docs/blob/main/.gitkeep"));
        _contentsService
            .Setup(service => service.FileExistsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _scaffolder = new GenesisStructureScaffolder(
            _projectRepository.Object,
            _tokenService.Object,
            _contentsService.Object,
            _pushFailureLogRepository.Object,
            _codeownersGenerator.Object,
            _markdownGenerator.Object,
            _versionProvider.Object,
            _timeProvider,
            _logger.Object);
    }

    [Fact]
    public async Task ScaffoldAsync_NoGitHubConfig_ReturnsFailureWithoutPushing()
    {
        var project = new Project(
            "TST", "Test Project", "desc", "PORTASK0001045",
            ComplianceDomain.Generic, "creator", TimeProvider.System);

        _projectRepository
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var result = await _scaffolder.ScaffoldAsync(project.Id, "test-user@emisgroup.com", CancellationToken.None);

        Assert.False(result.IsSuccess);

        _contentsService.Verify(service => service.PushFileAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ScaffoldAsync_AlreadyScaffolded_ReturnsWithoutPushing()
    {
        var project = CreateProject();
        _projectRepository
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _contentsService
            .Setup(service => service.FileExistsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                ".genesis/.gitkeep",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _scaffolder.ScaffoldAsync(project.Id, "test-user@emisgroup.com", CancellationToken.None);

        Assert.True(result.IsSuccess);

        _contentsService.Verify(service => service.PushFileAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ScaffoldAsync_NotYetScaffolded_PushesAllExpectedPaths()
    {
        var project = CreateProject();
        _projectRepository
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var result = await _scaffolder.ScaffoldAsync(project.Id, "test-user@emisgroup.com", CancellationToken.None);

        Assert.True(result.IsSuccess);

        var pushedPaths = _contentsService
            .Invocations
            .Where(call => call.Method.Name == nameof(IGitHubContentsService.PushFileAsync))
            .Select(call => (string)call.Arguments[3]!)
            .ToList();

        Assert.Equal(11, pushedPaths.Count);
        Assert.Contains(".genesis/requirements/.gitkeep", pushedPaths);
        Assert.Contains(".genesis/architecture/.gitkeep", pushedPaths);
        Assert.Contains(".genesis/clinical-safety/.gitkeep", pushedPaths);
        Assert.Contains(".genesis/ig/.gitkeep", pushedPaths);
        Assert.Contains(".genesis/security/.gitkeep", pushedPaths);
        Assert.Contains(".genesis/prototype/.gitkeep", pushedPaths);
        Assert.Contains(".genesis/session-close/.gitkeep", pushedPaths);
        Assert.Contains(".genesis/project/.gitkeep", pushedPaths);
        Assert.Contains(".genesis/CODEOWNERS", pushedPaths);
        Assert.Contains(".genesis/project/PROJECT.md", pushedPaths);
        Assert.Contains(".genesis/.gitkeep", pushedPaths);
    }

    [Fact]
    public async Task ScaffoldAsync_GitkeepPushedLast()
    {
        var project = CreateProject();
        _projectRepository
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var result = await _scaffolder.ScaffoldAsync(project.Id, "test-user@emisgroup.com", CancellationToken.None);

        Assert.True(result.IsSuccess);

        var pushedPaths = _contentsService
            .Invocations
            .Where(call => call.Method.Name == nameof(IGitHubContentsService.PushFileAsync))
            .Select(call => (string)call.Arguments[3]!)
            .ToList();

        Assert.Equal(".genesis/.gitkeep", pushedPaths.Last());
    }

    [Fact]
    public async Task ScaffoldAsync_CommitMessageContainsAllTrailers()
    {
        var project = CreateProject();
        _projectRepository
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var result = await _scaffolder.ScaffoldAsync(project.Id, "test-user@emisgroup.com", CancellationToken.None);

        Assert.True(result.IsSuccess);

        var commitMessage = _contentsService
            .Invocations
            .Where(call => call.Method.Name == nameof(IGitHubContentsService.PushFileAsync))
            .Select(call => (string)call.Arguments[5]!)
            .First();

        Assert.Contains("Provisioned-By: genesis-ai[bot]", commitMessage);
        Assert.Contains("Triggered-By: test-user@emisgroup.com", commitMessage);
        Assert.Contains("Project-ID:", commitMessage);
        Assert.Contains("Genesis-AI-Version: 1.0.0.0", commitMessage);
    }

    [Fact]
    public async Task ScaffoldAsync_PushFailure_ReturnsFailureAndLogsPushFailure()
    {
        var project = CreateProject();
        _projectRepository
            .Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var callCount = 0;
        _contentsService
            .Setup(service => service.PushFileAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 2)
                {
                    throw new InvalidOperationException("GitHub push failed");
                }
                return new GitHubPushResult("sha123", "https://github.com/...");
            });

        var result = await _scaffolder.ScaffoldAsync(project.Id, "test-user@emisgroup.com", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("GitHub push failed", result.FailureReason);
        _pushFailureLogRepository.Verify(
            repository => repository.AddAsync(
                It.Is<Genesis.AI.Domain.AggregatesModel.PushFailureLogAggregate.PushFailureLog>(
                    log => log.ProjectId == project.Id
                        && log.FilePath == ".genesis/scaffold"
                        && log.ErrorMessage == "GitHub push failed"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ScaffoldAsync_ProjectNotFound_ReturnsFailure()
    {
        var projectId = Guid.NewGuid();
        _projectRepository
            .Setup(repository => repository.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var result = await _scaffolder.ScaffoldAsync(projectId, "test-user@emisgroup.com", CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    private static Project CreateProject()
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
