using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.GitHub;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Services.GitHub;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Genesis.AI.Tests.Infrastructure.Services.GitHub;

public sealed class GenesisStructureScaffolderTests
{
    private readonly IProjectRepository _projectRepository;
    private readonly IGitHubTokenService _tokenService;
    private readonly IGitHubContentsService _contentsService;
    private readonly ICodeownersGenerator _codeownersGenerator;
    private readonly IProjectMarkdownGenerator _markdownGenerator;
    private readonly IAssemblyVersionProvider _versionProvider;
    private readonly ILogger<GenesisStructureScaffolder> _logger;
    private readonly GenesisStructureScaffolder _scaffolder;

    public GenesisStructureScaffolderTests()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _tokenService = Substitute.For<IGitHubTokenService>();
        _contentsService = Substitute.For<IGitHubContentsService>();
        _codeownersGenerator = Substitute.For<ICodeownersGenerator>();
        _markdownGenerator = Substitute.For<IProjectMarkdownGenerator>();
        _versionProvider = Substitute.For<IAssemblyVersionProvider>();
        _logger = Substitute.For<ILogger<GenesisStructureScaffolder>>();

        _tokenService
            .GetInstallationTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("token-abc");
        _versionProvider.GetVersion().Returns("1.0.0.0");
        _codeownersGenerator.Generate().Returns("# CODEOWNERS");
        _markdownGenerator.Generate(Arg.Any<Project>()).Returns("# PROJECT");
        _contentsService
            .PushFileAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(new GitHubPushResult("sha123", "https://github.com/emisgroup/emis-x-docs/blob/main/.gitkeep"));
        _contentsService
            .FileExistsAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        _scaffolder = new GenesisStructureScaffolder(
            _projectRepository,
            _tokenService,
            _contentsService,
            _codeownersGenerator,
            _markdownGenerator,
            _versionProvider,
            _logger);
    }

    [Fact]
    public async Task ScaffoldAsync_NoGitHubConfig_ReturnsWithoutPushing()
    {
        var project = new Project(
            "TST", "Test Project", "desc", "PORTASK0001045",
            ComplianceDomain.Generic, "creator", TimeProvider.System);

        _projectRepository
            .GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        await _scaffolder.ScaffoldAsync(project.Id, "test-user@emisgroup.com", CancellationToken.None);

        await _contentsService.DidNotReceive().PushFileAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScaffoldAsync_AlreadyScaffolded_ReturnsWithoutPushing()
    {
        var project = CreateProject();
        _projectRepository
            .GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);
        _contentsService
            .FileExistsAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                ".genesis/.gitkeep",
                Arg.Any<CancellationToken>())
            .Returns(true);

        await _scaffolder.ScaffoldAsync(project.Id, "test-user@emisgroup.com", CancellationToken.None);

        await _contentsService.DidNotReceive().PushFileAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScaffoldAsync_NotYetScaffolded_PushesAllExpectedPaths()
    {
        var project = CreateProject();
        _projectRepository
            .GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        await _scaffolder.ScaffoldAsync(project.Id, "test-user@emisgroup.com", CancellationToken.None);

        var pushedPaths = _contentsService
            .ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(IGitHubContentsService.PushFileAsync))
            .Select(call => (string)call.GetArguments()[3]!)
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
            .GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        await _scaffolder.ScaffoldAsync(project.Id, "test-user@emisgroup.com", CancellationToken.None);

        var pushedPaths = _contentsService
            .ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(IGitHubContentsService.PushFileAsync))
            .Select(call => (string)call.GetArguments()[3]!)
            .ToList();

        Assert.Equal(".genesis/.gitkeep", pushedPaths.Last());
    }

    [Fact]
    public async Task ScaffoldAsync_CommitMessageContainsAllTrailers()
    {
        var project = CreateProject();
        _projectRepository
            .GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        await _scaffolder.ScaffoldAsync(project.Id, "test-user@emisgroup.com", CancellationToken.None);

        var commitMessage = _contentsService
            .ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(IGitHubContentsService.PushFileAsync))
            .Select(call => (string)call.GetArguments()[5]!)
            .First();

        Assert.Contains("Provisioned-By: genesis-ai[bot]", commitMessage);
        Assert.Contains("Triggered-By: test-user@emisgroup.com", commitMessage);
        Assert.Contains("Project-ID:", commitMessage);
        Assert.Contains("Genesis-AI-Version: 1.0.0.0", commitMessage);
    }

    [Fact]
    public async Task ScaffoldAsync_PushFailure_DoesNotThrow()
    {
        var project = CreateProject();
        _projectRepository
            .GetByIdAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(project);

        var callCount = 0;
        _contentsService
            .PushFileAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callCount++;
                if (callCount == 2)
                {
                    throw new Exception("GitHub push failed");
                }
                return new GitHubPushResult("sha123", "https://github.com/...");
            });

        var exception = await Record.ExceptionAsync(() =>
            _scaffolder.ScaffoldAsync(project.Id, "test-user@emisgroup.com", CancellationToken.None));

        Assert.Null(exception);
    }

    [Fact]
    public async Task ScaffoldAsync_ProjectNotFound_DoesNotThrow()
    {
        var projectId = Guid.NewGuid();
        _projectRepository
            .GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        var exception = await Record.ExceptionAsync(() =>
            _scaffolder.ScaffoldAsync(projectId, "test-user@emisgroup.com", CancellationToken.None));

        Assert.Null(exception);
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
