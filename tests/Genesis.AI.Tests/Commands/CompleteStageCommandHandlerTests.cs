using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Commands.CompleteStage;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Moq;

namespace Genesis.AI.Tests.Commands;

public class CompleteStageCommandHandlerTests
{
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<IArtefactRepository> _artefactRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly TimeProvider _timeProvider;
    private readonly CompleteStageCommandHandler _handler;

    public CompleteStageCommandHandlerTests()
    {
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _artefactRepositoryMock = new Mock<IArtefactRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _timeProvider = TimeProvider.System;

        _projectRepositoryMock
            .Setup(r => r.UnitOfWork)
            .Returns(_unitOfWorkMock.Object);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new CompleteStageCommandHandler(
            _projectRepositoryMock.Object,
            _artefactRepositoryMock.Object,
            _timeProvider);
    }

    private Project CreateProjectWithInProgressStage(StageType stageType = StageType.RequirementsDiscovery)
    {
        var project = new Project("DOC", "Documents", null, ComplianceDomain.ClinicalUk, "user-1", _timeProvider);
        var stage = project.PipelineStages.First(s => s.StageType == stageType);
        stage.Start(_timeProvider);
        return project;
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ReturnsNotFound()
    {
        var stageId = Guid.NewGuid();
        _projectRepositoryMock
            .Setup(r => r.GetByStageIdAsync(stageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var command = new CompleteStageCommand(stageId, "user-1");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.Found);
    }

    [Fact]
    public async Task Handle_StageAlreadyComplete_ReturnsAlreadyComplete()
    {
        var project = CreateProjectWithInProgressStage();
        var stage = project.PipelineStages.First(s => s.StageType == StageType.RequirementsDiscovery);
        stage.Complete("user-1", _timeProvider);

        _projectRepositoryMock
            .Setup(r => r.GetByStageIdAsync(stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var command = new CompleteStageCommand(stage.Id, "user-1");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.Found);
        Assert.True(result.AlreadyComplete);
    }

    [Fact]
    public async Task Handle_StageNotInProgress_ReturnsValidationError()
    {
        var project = new Project("DOC", "Documents", null, ComplianceDomain.ClinicalUk, "user-1", _timeProvider);
        var stage = project.PipelineStages.First(s => s.StageType == StageType.RequirementsDiscovery);
        // Stage is NotStarted — should not be completable

        _projectRepositoryMock
            .Setup(r => r.GetByStageIdAsync(stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var command = new CompleteStageCommand(stage.Id, "user-1");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.Found);
        Assert.False(result.AlreadyComplete);
        Assert.NotNull(result.ValidationError);
        Assert.Contains("InProgress", result.ValidationError);
    }

    [Fact]
    public async Task Handle_NoArtefacts_ReturnsValidationError()
    {
        var project = CreateProjectWithInProgressStage();
        var stage = project.PipelineStages.First(s => s.StageType == StageType.RequirementsDiscovery);

        _projectRepositoryMock
            .Setup(r => r.GetByStageIdAsync(stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _artefactRepositoryMock
            .Setup(r => r.GetByProjectIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artefact>());

        var command = new CompleteStageCommand(stage.Id, "user-1");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.Found);
        Assert.NotNull(result.ValidationError);
        Assert.Contains("artefact", result.ValidationError);
    }

    [Fact]
    public async Task Handle_PrototypeStageWithNoArtefacts_ReturnsValidationError()
    {
        var project = CreateProjectWithInProgressStage(StageType.Prototype);
        var stage = project.PipelineStages.First(s => s.StageType == StageType.Prototype);

        _projectRepositoryMock
            .Setup(r => r.GetByStageIdAsync(stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _artefactRepositoryMock
            .Setup(r => r.GetByProjectIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artefact>());

        var command = new CompleteStageCommand(stage.Id, "user-1");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.Found);
        Assert.NotNull(result.ValidationError);
        Assert.Contains("artefact", result.ValidationError);
    }

    [Fact]
    public async Task Handle_ValidCompletion_CompletesStageAndSaves()
    {
        var project = CreateProjectWithInProgressStage();
        var stage = project.PipelineStages.First(s => s.StageType == StageType.RequirementsDiscovery);

        _projectRepositoryMock
            .Setup(r => r.GetByStageIdAsync(stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _artefactRepositoryMock
            .Setup(r => r.GetByProjectIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artefact> { CreateArtefact(project.Id) });

        var command = new CompleteStageCommand(stage.Id, "user-1");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.Found);
        Assert.False(result.AlreadyComplete);
        Assert.Null(result.ValidationError);
        Assert.Equal(stage.Id, result.StageId);
        Assert.Equal("complete", result.Status);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Artefact CreateArtefact(Guid projectId)
    {
        return Artefact.CreateTextArtefact(projectId, 1, "requirements.md", "text/markdown", "# Content", "user-1", TimeProvider.System);
    }
}
