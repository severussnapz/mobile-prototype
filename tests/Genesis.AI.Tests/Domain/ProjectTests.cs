using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Tests.Domain;

public class ProjectTests
{
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    [Fact]
    public void Constructor_ValidInputs_CreatesProjectWithCorrectProperties()
    {
        var project = new Project("DOC", "Documents Management", "A description", ComplianceDomain.ClinicalUk, "user-1", _timeProvider);

        Assert.NotEqual(Guid.Empty, project.Id);
        Assert.Equal("DOC", project.Code);
        Assert.Equal("Documents Management", project.Name);
        Assert.Equal("A description", project.Description);
        Assert.Equal(ComplianceDomain.ClinicalUk, project.ComplianceDomain);
        Assert.Equal(ProjectStatus.Discovery, project.Status);
        Assert.Equal("user-1", project.CreatedBy);
        Assert.False(project.IsDeleted);
    }

    [Fact]
    public void Constructor_WhenLowercaseCode_ConvertsToUppercase()
    {
        var project = new Project("doc", "Name", null, ComplianceDomain.Generic, "user-1", _timeProvider);

        Assert.Equal("DOC", project.Code);
    }

    [Fact]
    public void Constructor_WhenCreated_InitialisesEightStages()
    {
        var project = new Project("DOC", "Documents", null, ComplianceDomain.ClinicalUk, "user-1", _timeProvider);

        Assert.Equal(8, project.PipelineStages.Count);
    }

    [Fact]
    public void Constructor_ClinicalUkDomain_OnlyRequirementsDiscoveryNotStarted()
    {
        var project = new Project("DOC", "Documents", null, ComplianceDomain.ClinicalUk, "user-1", _timeProvider);

        var requirementsStage = project.PipelineStages.First(stage => stage.StageType == StageType.RequirementsDiscovery);
        Assert.Equal(PipelineStageStatus.NotStarted, requirementsStage.Status);

        var otherStages = project.PipelineStages.Where(stage => stage.StageType != StageType.RequirementsDiscovery);
        Assert.All(otherStages, stage =>
            Assert.Equal(PipelineStageStatus.Blocked, stage.Status));
    }

    [Fact]
    public void Constructor_GenericDomain_OnlyRequirementsDiscoveryNotStarted()
    {
        var project = new Project("DOC", "Documents", null, ComplianceDomain.Generic, "user-1", _timeProvider);

        var requirementsStage = project.PipelineStages.First(stage => stage.StageType == StageType.RequirementsDiscovery);
        Assert.Equal(PipelineStageStatus.NotStarted, requirementsStage.Status);

        var otherStages = project.PipelineStages.Where(stage => stage.StageType != StageType.RequirementsDiscovery);
        Assert.All(otherStages, stage =>
            Assert.Equal(PipelineStageStatus.Blocked, stage.Status));
    }

    [Fact]
    public void Constructor_FinanceDomain_OnlyRequirementsDiscoveryNotStarted()
    {
        var project = new Project("FIN", "Finance", null, ComplianceDomain.Finance, "user-1", _timeProvider);

        var requirementsStage = project.PipelineStages.First(stage => stage.StageType == StageType.RequirementsDiscovery);
        Assert.Equal(PipelineStageStatus.NotStarted, requirementsStage.Status);

        var otherStages = project.PipelineStages.Where(stage => stage.StageType != StageType.RequirementsDiscovery);
        Assert.All(otherStages, stage =>
            Assert.Equal(PipelineStageStatus.Blocked, stage.Status));
    }

    [Fact]
    public void Constructor_WhenCreated_StagesHaveCorrectSortOrder()
    {
        var project = new Project("DOC", "Documents", null, ComplianceDomain.ClinicalUk, "user-1", _timeProvider);

        var stages = project.PipelineStages.OrderBy(s => s.SortOrder).ToList();
        Assert.Equal(StageType.RequirementsDiscovery, stages[0].StageType);
        Assert.Equal(1, stages[0].SortOrder);
        Assert.Equal(StageType.Planning, stages[^1].StageType);
    }

    [Fact]
    public void SoftDelete_WhenActive_SetsIsDeletedTrue()
    {
        var project = new Project("DOC", "Documents", null, ComplianceDomain.Generic, "user-1", _timeProvider);

        project.SoftDelete(_timeProvider);

        Assert.True(project.IsDeleted);
    }

    [Fact]
    public void SoftDelete_WhenActive_UpdatesTimestamp()
    {
        var project = new Project("DOC", "Documents", null, ComplianceDomain.Generic, "user-1", _timeProvider);
        var originalUpdatedAt = project.UpdatedAt;

        project.SoftDelete(_timeProvider);

        Assert.True(project.UpdatedAt >= originalUpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrEmptyCode_ThrowsArgumentException(string? code)
    {
        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(
            () => new Project(code!, "Name", null, ComplianceDomain.Generic, "user-1", _timeProvider));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrEmptyName_ThrowsArgumentException(string? name)
    {
        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(
            () => new Project("DOC", name!, null, ComplianceDomain.Generic, "user-1", _timeProvider));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrEmptyCreatedBy_ThrowsArgumentException(string? createdBy)
    {
        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(
            () => new Project("DOC", "Name", null, ComplianceDomain.Generic, createdBy!, _timeProvider));
    }

    [Fact]
    public void RecalculateStatus_RequirementsDiscoveryComplete_UnblocksPrototype()
    {
        var project = new Project("DOC", "Documents", null, ComplianceDomain.ClinicalUk, "user-1", _timeProvider);
        var reqStage = project.PipelineStages.First(stage => stage.StageType == StageType.RequirementsDiscovery);

        reqStage.Start(_timeProvider);
        reqStage.Complete("user-1", _timeProvider);
        project.RecalculateStatus(_timeProvider);

        var protoStage = project.PipelineStages.First(stage => stage.StageType == StageType.Prototype);
        Assert.Equal(PipelineStageStatus.NotStarted, protoStage.Status);
    }

    [Fact]
    public void RecalculateStatus_PrototypeComplete_UnblocksArchDesignPxd()
    {
        var project = new Project("DOC", "Documents", null, ComplianceDomain.ClinicalUk, "user-1", _timeProvider);
        CompleteStage(project, StageType.RequirementsDiscovery);
        CompleteStage(project, StageType.Prototype);

        project.RecalculateStatus(_timeProvider);

        Assert.Equal(PipelineStageStatus.NotStarted, GetStage(project, StageType.Architecture).Status);
        Assert.Equal(PipelineStageStatus.NotStarted, GetStage(project, StageType.Design).Status);
        Assert.Equal(PipelineStageStatus.NotStarted, GetStage(project, StageType.Pxd).Status);
    }

    [Fact]
    public void RecalculateStatus_PrototypeComplete_ClinicalSafetyStillBlocked()
    {
        var project = new Project("DOC", "Documents", null, ComplianceDomain.ClinicalUk, "user-1", _timeProvider);
        CompleteStage(project, StageType.RequirementsDiscovery);
        CompleteStage(project, StageType.Prototype);

        project.RecalculateStatus(_timeProvider);

        Assert.Equal(PipelineStageStatus.Blocked, GetStage(project, StageType.ClinicalSafety).Status);
    }

    [Fact]
    public void RecalculateStatus_ArchDesignPxdComplete_UnblocksClinicalSafety()
    {
        var project = new Project("DOC", "Documents", null, ComplianceDomain.ClinicalUk, "user-1", _timeProvider);
        CompleteStage(project, StageType.RequirementsDiscovery);
        CompleteStage(project, StageType.Prototype);
        CompleteStage(project, StageType.Architecture);
        CompleteStage(project, StageType.Design);
        CompleteStage(project, StageType.Pxd);

        project.RecalculateStatus(_timeProvider);

        Assert.Equal(PipelineStageStatus.NotStarted, GetStage(project, StageType.ClinicalSafety).Status);
    }

    [Fact]
    public void RecalculateStatus_NonClinicalDomain_ClinicalSafetyNeverUnblocks()
    {
        var project = new Project("DOC", "Documents", null, ComplianceDomain.Generic, "user-1", _timeProvider);
        CompleteStage(project, StageType.RequirementsDiscovery);
        CompleteStage(project, StageType.Prototype);
        CompleteStage(project, StageType.Architecture);
        CompleteStage(project, StageType.Design);
        CompleteStage(project, StageType.Pxd);

        project.RecalculateStatus(_timeProvider);

        Assert.Equal(PipelineStageStatus.Blocked, GetStage(project, StageType.ClinicalSafety).Status);
    }

    [Fact]
    public void RecalculateStatus_NonClinicalDomain_NormalisationUnblocksWhenClinicalSafetyBlocked()
    {
        var project = new Project("DOC", "Documents", null, ComplianceDomain.Generic, "user-1", _timeProvider);
        CompleteStage(project, StageType.RequirementsDiscovery);
        CompleteStage(project, StageType.Prototype);
        CompleteStage(project, StageType.Architecture);
        CompleteStage(project, StageType.Design);
        CompleteStage(project, StageType.Pxd);

        project.RecalculateStatus(_timeProvider);

        // ClinicalSafety stays blocked for non-clinical, so Normalisation should unblock
        Assert.Equal(PipelineStageStatus.NotStarted, GetStage(project, StageType.Normalisation).Status);
    }

    [Fact]
    public void RecalculateStatus_ClinicalSafetyComplete_UnblocksNormalisation()
    {
        var project = new Project("DOC", "Documents", null, ComplianceDomain.ClinicalUk, "user-1", _timeProvider);
        CompleteStage(project, StageType.RequirementsDiscovery);
        CompleteStage(project, StageType.Prototype);
        CompleteStage(project, StageType.Architecture);
        CompleteStage(project, StageType.Design);
        CompleteStage(project, StageType.Pxd);
        CompleteStage(project, StageType.ClinicalSafety);

        project.RecalculateStatus(_timeProvider);

        Assert.Equal(PipelineStageStatus.NotStarted, GetStage(project, StageType.Normalisation).Status);
    }

    [Fact]
    public void RecalculateStatus_NormalisationComplete_UnblocksPlanning()
    {
        var project = new Project("DOC", "Documents", null, ComplianceDomain.ClinicalUk, "user-1", _timeProvider);
        CompleteStage(project, StageType.RequirementsDiscovery);
        CompleteStage(project, StageType.Prototype);
        CompleteStage(project, StageType.Architecture);
        CompleteStage(project, StageType.Design);
        CompleteStage(project, StageType.Pxd);
        CompleteStage(project, StageType.ClinicalSafety);
        CompleteStage(project, StageType.Normalisation);

        project.RecalculateStatus(_timeProvider);

        Assert.Equal(PipelineStageStatus.NotStarted, GetStage(project, StageType.Planning).Status);
    }

    [Fact]
    public void RecalculateStatus_AllStagesComplete_ProjectStatusComplete()
    {
        var project = new Project("DOC", "Documents", null, ComplianceDomain.ClinicalUk, "user-1", _timeProvider);
        CompleteStage(project, StageType.RequirementsDiscovery);
        CompleteStage(project, StageType.Prototype);
        CompleteStage(project, StageType.Architecture);
        CompleteStage(project, StageType.Design);
        CompleteStage(project, StageType.Pxd);
        CompleteStage(project, StageType.ClinicalSafety);
        CompleteStage(project, StageType.Normalisation);
        CompleteStage(project, StageType.Planning);

        project.RecalculateStatus(_timeProvider);

        Assert.Equal(ProjectStatus.Complete, project.Status);
    }

    private static void CompleteStage(Project project, StageType stageType)
    {
        var stage = project.PipelineStages.First(pipelineStage => pipelineStage.StageType == stageType);
        if (stage.Status == PipelineStageStatus.Blocked)
        {
            stage.Unblock();
        }
        stage.Start(TimeProvider.System);
        stage.Complete("user-1", TimeProvider.System);
        project.RecalculateStatus(TimeProvider.System);
    }

    private static PipelineStage GetStage(Project project, StageType stageType) =>
        project.PipelineStages.First(pipelineStage => pipelineStage.StageType == stageType);
}
