using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Commands.UpdateProject;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Moq;

namespace Genesis.AI.Tests.Application.UpdateProject;

public sealed class UpdateProjectCommandHandlerTests
{
    private readonly Mock<IProjectRepository> _projectRepository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ISecretEncryptionService> _secretEncryptionService;
    private readonly Mock<IGenesisStructureScaffolder> _scaffolder;
    private readonly TimeProvider _timeProvider;
    private readonly UpdateProjectCommandHandler _handler;

    public UpdateProjectCommandHandlerTests()
    {
        _projectRepository = new Mock<IProjectRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _secretEncryptionService = new Mock<ISecretEncryptionService>();
        _scaffolder = new Mock<IGenesisStructureScaffolder>();
        _timeProvider = TimeProvider.System;

        _projectRepository.SetupGet(repository => repository.UnitOfWork).Returns(_unitOfWork.Object);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _scaffolder.Setup(scaffolder => scaffolder.ScaffoldAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ScaffoldResult.Success());

        _handler = new UpdateProjectCommandHandler(
            _projectRepository.Object,
            _secretEncryptionService.Object,
            _scaffolder.Object,
            _timeProvider);
    }

    [Fact]
    public async Task Handle_WithFigmaPat_EncryptsBeforeSave()
    {
        var project = CreateProject();
        var command = CreateCommand(project.Id) with { FigmaPat = "plaintext-pat" };

        _projectRepository.Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        _secretEncryptionService.Setup(service => service.Encrypt("plaintext-pat")).Returns("encrypted-value");

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("encrypted-value", project.FigmaPatEncrypted);
        Assert.NotEqual("plaintext-pat", project.FigmaPatEncrypted);
    }

    [Fact]
    public async Task Handle_WithFigmaPat_ReturnsPlaintextOnceInResponse()
    {
        var project = CreateProject();
        var command = CreateCommand(project.Id) with { FigmaPat = "plaintext-pat" };

        _projectRepository.Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        _secretEncryptionService.Setup(service => service.Encrypt("plaintext-pat")).Returns("encrypted-value");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("plaintext-pat", result.FigmaPatPlaintext);
    }

    [Fact]
    public async Task Handle_NullFigmaPat_DoesNotCallEncrypt()
    {
        var project = CreateProject();
        var command = CreateCommand(project.Id) with { FigmaPat = null };

        _projectRepository.Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);

        await _handler.Handle(command, CancellationToken.None);

        _secretEncryptionService.Verify(service => service.Encrypt(It.IsAny<string>()), Times.Never);
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

        _projectRepository.Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);

        await _handler.Handle(command, CancellationToken.None);

        _scaffolder.Verify(scaffolder => scaffolder.ScaffoldAsync(project.Id, command.UpdatedBy, It.IsAny<CancellationToken>()), Times.Once);
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

        _projectRepository.Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);

        await _handler.Handle(command, CancellationToken.None);

        _scaffolder.Verify(scaffolder => scaffolder.ScaffoldAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
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

        _projectRepository.Setup(repository => repository.GetByIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        _scaffolder
            .Setup(scaffolder => scaffolder.ScaffoldAsync(project.Id, command.UpdatedBy, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

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