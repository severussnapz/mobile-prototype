using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Tests.Domain;

public class PipelineStageTests
{
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    private PipelineStage CreateStage(
        StageType stageType = StageType.RequirementsDiscovery,
        PipelineStageStatus status = PipelineStageStatus.NotStarted)
    {
        var project = new Project("DOC", "Documents", null, ComplianceDomain.ClinicalUk, "user-1", _timeProvider);
        return project.PipelineStages.First(s => s.StageType == stageType);
    }

    [Fact]
    public void Start_WhenNotStarted_SetsStatusToInProgress()
    {
        var stage = CreateStage();

        stage.Start(_timeProvider);

        Assert.Equal(PipelineStageStatus.InProgress, stage.Status);
    }

    [Fact]
    public void Start_WhenNotStarted_SetsStartedAt()
    {
        var stage = CreateStage();

        stage.Start(_timeProvider);

        Assert.NotNull(stage.StartedAt);
    }

    [Fact]
    public void Complete_WhenInProgress_SetsStatusToComplete()
    {
        var stage = CreateStage();
        stage.Start(_timeProvider);

        stage.Complete("user-1", _timeProvider);

        Assert.Equal(PipelineStageStatus.Complete, stage.Status);
    }

    [Fact]
    public void Complete_WhenInProgress_SetsCompletedAtAndCompletedBy()
    {
        var stage = CreateStage();
        stage.Start(_timeProvider);

        stage.Complete("admin", _timeProvider);

        Assert.NotNull(stage.CompletedAt);
        Assert.Equal("admin", stage.CompletedBy);
    }

    [Fact]
    public void Skip_WhenOptional_SetsStatusToComplete()
    {
        var stage = CreateStage(StageType.Prototype);

        stage.Skip();

        Assert.Equal(PipelineStageStatus.Complete, stage.Status);
    }

    [Fact]
    public void Reopen_WhenComplete_SetsStatusToInProgress()
    {
        var stage = CreateStage();
        stage.Start(_timeProvider);
        stage.Complete("user-1", _timeProvider);

        stage.Reopen(_timeProvider);

        Assert.Equal(PipelineStageStatus.InProgress, stage.Status);
    }

    [Fact]
    public void Reopen_WhenComplete_IncrementsIteration()
    {
        var stage = CreateStage();
        stage.Start(_timeProvider);
        stage.Complete("user-1", _timeProvider);

        stage.Reopen(_timeProvider);

        Assert.Equal(2, stage.Iteration);
    }

    [Fact]
    public void Reopen_WhenComplete_ClearsCompletedFields()
    {
        var stage = CreateStage();
        stage.Start(_timeProvider);
        stage.Complete("user-1", _timeProvider);

        stage.Reopen(_timeProvider);

        Assert.Null(stage.CompletedAt);
        Assert.Null(stage.CompletedBy);
    }

    [Fact]
    public void Reopen_WhenComplete_UpdatesStartedAt()
    {
        var stage = CreateStage();
        stage.Start(_timeProvider);
        var firstStartedAt = stage.StartedAt;
        stage.Complete("user-1", _timeProvider);

        stage.Reopen(_timeProvider);

        Assert.NotNull(stage.StartedAt);
        Assert.True(stage.StartedAt >= firstStartedAt);
    }

    [Fact]
    public void Block_WhenNotStarted_SetsStatusToBlocked()
    {
        var stage = CreateStage();

        stage.Block();

        Assert.Equal(PipelineStageStatus.Blocked, stage.Status);
    }

    [Fact]
    public void InitialIteration_WhenCreated_IsOne()
    {
        // Arrange & Act
        var stage = CreateStage();

        Assert.Equal(1, stage.Iteration);
    }
}
