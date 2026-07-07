using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Commands.UpdateProject;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using NSubstitute;

namespace Genesis.AI.Tests.Application.UpdateProject;

public sealed class UpdateProjectCommandHandlerTests
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecretEncryptionService _secretEncryptionService;
    private readonly IGenesisStructureScaffolder _scaffolder;
    private readonly TimeProvider _timeProvider;
    private readonly UpdateProjectCommandHandler _handler;

    public UpdateProjectCommandHandlerTests()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _secretEncryptionService = Substitute.For<ISecretEncryptionService>();
        _scaffolder = Substitute.For<IGenesisStructureScaffolder>();
        _timeProvider = TimeProvider.System;

        _projectRepository.UnitOfWork.Returns(_unitOfWork);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        _handler = new UpdateProjectCommandHandler(
            _projectRepository,
            _secretEncryptionService,
            _scaffolder,
            _timeProvider);
    }

    [Fact]
    public async Task Handle_WithFigmaPat_EncryptsBeforeSave()
    {
        var project = CreateProject();
        var command = CreateCommand(project.Id) with { FigmaPat = "plaintext-pat" };

        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _secretEncryptionService.Encrypt("plaintext-pat").Returns("encrypted-value");

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("encrypted-value", project.FigmaPatEncrypted);
        Assert.NotEqual("plaintext-pat", project.FigmaPatEncrypted);
    }

    [Fact]
    public async Task Handle_WithFigmaPat_ReturnsPlaintextOnceInResponse()
    {
        var project = CreateProject();
        var command = CreateCommand(project.Id) with { FigmaPat = "plaintext-pat" };

        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _secretEncryptionService.Encrypt("plaintext-pat").Returns("encrypted-value");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("plaintext-pat", result.FigmaPatPlaintext);
    }

    [Fact]
    public async Task Handle_NullFigmaPat_DoesNotCallEncrypt()
    {
        var project = CreateProject();
        var command = CreateCommand(project.Id) with { FigmaPat = null };

        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        await _handler.Handle(command, CancellationToken.None);

        _secretEncryptionService.DidNotReceive().Encrypt(Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_GitHubInstallationIdTransitionsFromNull_TriggersScaffold()
    {
        var project = CreateProject();
        var command = CreateCommand(project.Id) with
        {
            GitHubApiRepoUrl = "https://github.com/emisgroup/emis-x-documents-api",
            GitHubAppRepoUrl = "https://github.com/emisgroup/emis-x-documents-app",
            GitHubRepoOwner = "emisgroup",
            GitHubRepoName = "emis-x-documents",
            GitHubInstallationId = "12345678"
        };

        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        await _handler.Handle(command, CancellationToken.None);

        await _scaffolder.Received(1).ScaffoldAsync(project.Id, command.UpdatedBy, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GitHubInstallationIdAlreadySet_DoesNotTriggerScaffold()
    {
        var project = CreateProject();
        project.SetGitHubConfig(
            "https://github.com/emisgroup/emis-x-documents-api",
            "https://github.com/emisgroup/emis-x-documents-app",
            "emisgroup",
            "emis-x-documents",
            "already-set",
            _timeProvider);

        var command = CreateCommand(project.Id) with
        {
            GitHubApiRepoUrl = "https://github.com/emisgroup/emis-x-documents-api",
            GitHubAppRepoUrl = "https://github.com/emisgroup/emis-x-documents-app",
            GitHubRepoOwner = "emisgroup",
            GitHubRepoName = "emis-x-documents",
            GitHubInstallationId = "different-id"
        };

        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        await _handler.Handle(command, CancellationToken.None);

        await _scaffolder.DidNotReceive().ScaffoldAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ScaffoldThrows_DoesNotFailHandler()
    {
        var project = CreateProject();
        var command = CreateCommand(project.Id) with
        {
            GitHubApiRepoUrl = "https://github.com/emisgroup/emis-x-documents-api",
            GitHubAppRepoUrl = "https://github.com/emisgroup/emis-x-documents-app",
            GitHubRepoOwner = "emisgroup",
            GitHubRepoName = "emis-x-documents",
            GitHubInstallationId = "12345678"
        };

        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _scaffolder
            .ScaffoldAsync(project.Id, command.UpdatedBy, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new Exception("boom")));

        var exception = await Record.ExceptionAsync(() => _handler.Handle(command, CancellationToken.None));

        Assert.Null(exception);
    }

    private static Project CreateProject()
    {
        return new Project(
            "DOC",
            "Documents",
            "A project",
            "PORTASK0001045",
            ComplianceDomain.ClinicalUk,
            "user-1",
            TimeProvider.System);
    }

    private static UpdateProjectCommand CreateCommand(Guid projectId)
    {
        return new UpdateProjectCommand(
            projectId,
            "Updated name",
            "Updated description",
            "PORTASK0001045",
            ComplianceDomain.ClinicalUk,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            "user-1");
    }
}