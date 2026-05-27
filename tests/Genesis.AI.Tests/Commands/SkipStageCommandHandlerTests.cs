using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Commands.SkipStage;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Moq;

namespace Genesis.AI.Tests.Commands;

public class SkipStageCommandHandlerTests
{
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly TimeProvider _timeProvider;
    private readonly SkipStageCommandHandler _handler;

    public SkipStageCommandHandlerTests()
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

        _handler = new SkipStageCommandHandler(_projectRepositoryMock.Object, _timeProvider);
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ReturnsNotFound()
    {
        var stageId = Guid.NewGuid();
        _projectRepositoryMock
            .Setup(r => r.GetByStageIdAsync(stageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var command = new SkipStageCommand(stageId);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.Found);
    }

    [Fact]
    public async Task Handle_CompletedStage_ReturnsValidationError()
    {
        var project = new Project("DOC", "Documents", null, ComplianceDomain.ClinicalUk, "user-1", _timeProvider);
        var stage = project.PipelineStages.First(s => s.StageType == StageType.Prototype);
        stage.Start(_timeProvider);
        stage.Complete("user-1", _timeProvider);

        _projectRepositoryMock
            .Setup(r => r.GetByStageIdAsync(stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var command = new SkipStageCommand(stage.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.Found);
        Assert.NotNull(result.ValidationError);
        Assert.Contains("completed", result.ValidationError);
    }

    [Fact]
    public async Task Handle_NotStartedStage_SkipsSuccessfully()
    {
        var project = new Project("DOC", "Documents", null, ComplianceDomain.ClinicalUk, "user-1", _timeProvider);
        var stage = project.PipelineStages.First(s => s.StageType == StageType.Prototype);

        _projectRepositoryMock
            .Setup(r => r.GetByStageIdAsync(stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var command = new SkipStageCommand(stage.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.Found);
        Assert.Null(result.ValidationError);
        Assert.Equal("complete", result.Status);
        Assert.Equal(stage.Id, result.StageId);
    }

    [Fact]
    public async Task Handle_SkipsStage_SavesChanges()
    {
        var project = new Project("DOC", "Documents", null, ComplianceDomain.ClinicalUk, "user-1", _timeProvider);
        var stage = project.PipelineStages.First(s => s.StageType == StageType.Prototype);

        _projectRepositoryMock
            .Setup(r => r.GetByStageIdAsync(stage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var command = new SkipStageCommand(stage.Id);

        await _handler.Handle(command, CancellationToken.None);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
