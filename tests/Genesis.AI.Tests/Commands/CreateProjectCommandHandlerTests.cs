using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Commands.CreateProject;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Moq;

namespace Genesis.AI.Tests.Commands;

public class CreateProjectCommandHandlerTests
{
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly TimeProvider _timeProvider;
    private readonly CreateProjectCommandHandler _handler;

    public CreateProjectCommandHandlerTests()
    {
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _timeProvider = TimeProvider.System;

        _projectRepositoryMock
            .Setup(r => r.UnitOfWork)
            .Returns(_unitOfWorkMock.Object);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new CreateProjectCommandHandler(
            _projectRepositoryMock.Object,
            _timeProvider);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsNewProjectId()
    {
        var command = new CreateProjectCommand("DOC", "Documents Management", "Description", "PORTASK0001045", ComplianceDomain.ClinicalUk, "user-1");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsAddAsync()
    {
        Project? capturedProject = null;
        _projectRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Callback<Project, CancellationToken>((project, _) => capturedProject = project)
            .Returns(Task.CompletedTask);

        var command = new CreateProjectCommand("DOC", "Documents Management", null, "PORTASK0001045", ComplianceDomain.Generic, "user-1");

        await _handler.Handle(command, CancellationToken.None);

        _projectRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.NotNull(capturedProject);
        Assert.Equal("Documents Management", capturedProject.Name);
        Assert.Equal("DOC", capturedProject.Code);
    }

    [Fact]
    public async Task Handle_ValidCommand_SavesChanges()
    {
        var command = new CreateProjectCommand("DOC", "Documents Management", null, "PORTASK0001045", ComplianceDomain.Generic, "user-1");

        await _handler.Handle(command, CancellationToken.None);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenValidCommand_CreatesProjectWithCorrectProperties()
    {
        Project? capturedProject = null;
        _projectRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Callback<Project, CancellationToken>((p, _) => capturedProject = p)
            .Returns(Task.CompletedTask);

        var command = new CreateProjectCommand("TEST", "Test Project", "A description", "PORTASK0001045", ComplianceDomain.ClinicalUk, "admin");

        await _handler.Handle(command, CancellationToken.None);

        Assert.NotNull(capturedProject);
        Assert.Equal("TEST", capturedProject.Code);
        Assert.Equal("Test Project", capturedProject.Name);
        Assert.Equal("A description", capturedProject.Description);
        Assert.Equal("PORTASK0001045", capturedProject.TimeSheetCode);
        Assert.Equal(ComplianceDomain.ClinicalUk, capturedProject.ComplianceDomain);
        Assert.Equal("admin", capturedProject.CreatedBy);
        Assert.Equal(ProjectStatus.Discovery, capturedProject.Status);
        Assert.False(capturedProject.IsDeleted);
    }

    [Fact]
    public async Task Handle_WhenValidCommand_InitialisesEightPipelineStages()
    {
        Project? capturedProject = null;
        _projectRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Callback<Project, CancellationToken>((p, _) => capturedProject = p)
            .Returns(Task.CompletedTask);

        var command = new CreateProjectCommand("DOC", "Documents", null, "PORTASK0001045", ComplianceDomain.ClinicalUk, "user-1");

        await _handler.Handle(command, CancellationToken.None);

        Assert.NotNull(capturedProject);
        Assert.Equal(8, capturedProject.PipelineStages.Count);
    }

    [Fact]
    public async Task Handle_ClinicalUkDomain_OnlyRequirementsDiscoveryNotStarted()
    {
        Project? capturedProject = null;
        _projectRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Callback<Project, CancellationToken>((p, _) => capturedProject = p)
            .Returns(Task.CompletedTask);

        var command = new CreateProjectCommand("DOC", "Documents", null, "PORTASK0001045", ComplianceDomain.ClinicalUk, "user-1");

        await _handler.Handle(command, CancellationToken.None);

        Assert.NotNull(capturedProject);
        var reqStage = capturedProject.PipelineStages.First(stage => stage.StageType == StageType.RequirementsDiscovery);
        Assert.Equal(PipelineStageStatus.NotStarted, reqStage.Status);

        var otherStages = capturedProject.PipelineStages.Where(stage => stage.StageType != StageType.RequirementsDiscovery);
        Assert.All(otherStages, stage =>
            Assert.Equal(PipelineStageStatus.Blocked, stage.Status));
    }

    [Fact]
    public async Task Handle_GenericDomain_OnlyRequirementsDiscoveryNotStarted()
    {
        Project? capturedProject = null;
        _projectRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Callback<Project, CancellationToken>((p, _) => capturedProject = p)
            .Returns(Task.CompletedTask);

        var command = new CreateProjectCommand("DOC", "Documents", null, "PORTASK0001045", ComplianceDomain.Generic, "user-1");

        await _handler.Handle(command, CancellationToken.None);

        Assert.NotNull(capturedProject);
        var reqStage = capturedProject.PipelineStages.First(stage => stage.StageType == StageType.RequirementsDiscovery);
        Assert.Equal(PipelineStageStatus.NotStarted, reqStage.Status);

        var otherStages = capturedProject.PipelineStages.Where(stage => stage.StageType != StageType.RequirementsDiscovery);
        Assert.All(otherStages, stage =>
            Assert.Equal(PipelineStageStatus.Blocked, stage.Status));
    }
}
