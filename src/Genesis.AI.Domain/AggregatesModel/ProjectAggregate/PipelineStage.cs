using Genesis.AI.Core.Domain;
using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Domain.AggregatesModel.ProjectAggregate;

/// <summary>
/// Represents a single stage within a project's processing pipeline.
/// </summary>
public class PipelineStage : Entity
{
    public Guid ProjectId { get; private set; }
    public StageType StageType { get; private set; }
    public PipelineStageStatus Status { get; private set; }
    public int SortOrder { get; private set; }
    public int Iteration { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? CompletedBy { get; private set; }

    private PipelineStage() { } // Required for EF Core

    internal PipelineStage(Guid projectId, StageType stageType, PipelineStageStatus status, int sortOrder)
    {
        Id = Guid.NewGuid();
        ProjectId = projectId;
        StageType = stageType;
        Status = status;
        SortOrder = sortOrder;
        Iteration = 1;
    }

    public void Start(TimeProvider timeProvider)
    {
        Status = PipelineStageStatus.InProgress;
        StartedAt = timeProvider.GetUtcNow();
    }

    public void Complete(string completedBy, TimeProvider timeProvider)
    {
        Status = PipelineStageStatus.Complete;
        CompletedAt = timeProvider.GetUtcNow();
        CompletedBy = completedBy;
    }

    public void Block()
    {
        Status = PipelineStageStatus.Blocked;
    }

    public void Unblock()
    {
        if (Status == PipelineStageStatus.Blocked)
        {
            Status = PipelineStageStatus.NotStarted;
        }
    }

    public void Skip()
    {
        Status = PipelineStageStatus.Complete;
    }

    public void Reopen(TimeProvider timeProvider)
    {
        Status = PipelineStageStatus.InProgress;
        Iteration++;
        StartedAt = timeProvider.GetUtcNow();
        CompletedAt = null;
        CompletedBy = null;
    }
}
