using Genesis.AI.Core.Domain;
using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Domain.AggregatesModel.ProjectAggregate;

/// <summary>
/// Aggregate root representing a requirements project with its pipeline stages.
/// </summary>
public class Project : Entity, IAggregateRoot
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string TimeSheetCode { get; private set; } = null!;
    public ComplianceDomain ComplianceDomain { get; private set; }
    public ProjectStatus Status { get; private set; }
    public string CreatedBy { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }

    private readonly List<PipelineStage> _pipelineStages = [];
    public IReadOnlyCollection<PipelineStage> PipelineStages => _pipelineStages.AsReadOnly();

    private Project() { } // Required for EF Core

    public Project(
        string code,
        string name,
        string? description,
        string timeSheetCode,
        ComplianceDomain complianceDomain,
        string createdBy,
        TimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(timeSheetCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        Id = Guid.NewGuid();
        Code = code.ToUpperInvariant();
        Name = name;
        Description = description;
        TimeSheetCode = timeSheetCode;
        ComplianceDomain = complianceDomain;
        Status = ProjectStatus.Discovery;
        CreatedBy = createdBy;
        CreatedAt = timeProvider.GetUtcNow();
        UpdatedAt = CreatedAt;
        IsDeleted = false;

        InitialisePipelineStages();
    }

    public void SoftDelete(TimeProvider timeProvider)
    {
        IsDeleted = true;
        UpdatedAt = timeProvider.GetUtcNow();
    }

    public void RecalculateStatus(TimeProvider timeProvider)
    {
        UnblockAvailableStages();

        var activeStages = _pipelineStages
            .Where(stage => stage.Status != PipelineStageStatus.Blocked);

        var allComplete = activeStages.All(stage =>
            stage.Status == PipelineStageStatus.Complete);

        var anyInProgress = activeStages.Any(stage =>
            stage.Status == PipelineStageStatus.InProgress);

        var anyComplete = activeStages.Any(stage =>
            stage.Status == PipelineStageStatus.Complete);

        var newStatus = allComplete
            ? ProjectStatus.Complete
            : (anyInProgress || anyComplete)
                ? ProjectStatus.InProgress
                : ProjectStatus.Discovery;

        if (newStatus != Status)
        {
            Status = newStatus;
            UpdatedAt = timeProvider.GetUtcNow();
        }
    }

    /// <summary>
    /// Checks prerequisites and unblocks stages whose dependencies are now satisfied.
    /// Prerequisite chain:
    ///   RequirementsDiscovery → Prototype → [Architecture, Design, PxD] → ClinicalSafety → InformationGovernance → Security → Normalisation → Planning
    /// ClinicalSafety is also permanently blocked for non-clinical domains.
    /// </summary>
    private void UnblockAvailableStages()
    {
        foreach (var stage in _pipelineStages.Where(stage => stage.Status == PipelineStageStatus.Blocked))
        {
            var shouldUnblock = stage.StageType switch
            {
                StageType.Prototype => IsStageComplete(StageType.RequirementsDiscovery),

                StageType.Architecture or StageType.Design or StageType.Pxd
                    => IsStageComplete(StageType.Prototype),

                StageType.ClinicalSafety
                    => ComplianceDomain == ComplianceDomain.ClinicalUk
                       && IsStageComplete(StageType.Architecture)
                       && IsStageComplete(StageType.Design)
                       && IsStageComplete(StageType.Pxd),

                StageType.InformationGovernance
                    => IsStageComplete(StageType.Architecture)
                       && IsStageComplete(StageType.Design)
                       && IsStageComplete(StageType.Pxd)
                       && (IsStageComplete(StageType.ClinicalSafety)
                           || ComplianceDomain != ComplianceDomain.ClinicalUk),

                StageType.Security
                    => IsStageComplete(StageType.InformationGovernance),

                StageType.Normalisation
                    => IsStageComplete(StageType.Security),

                StageType.Planning
                    => IsStageComplete(StageType.Normalisation),

                _ => false
            };

            if (shouldUnblock)
            {
                stage.Unblock();
            }
        }
    }

    private bool IsStageComplete(StageType stageType)
    {
        var stage = _pipelineStages.FirstOrDefault(pipelineStage => pipelineStage.StageType == stageType);
        return stage?.Status == PipelineStageStatus.Complete;
    }

    private void InitialisePipelineStages()
    {
        var stageTypes = Enum.GetValues<StageType>();

        foreach (var stageType in stageTypes)
        {
            var status = stageType switch
            {
                // Only RequirementsDiscovery is available from the start
                StageType.RequirementsDiscovery => PipelineStageStatus.NotStarted,
                // Everything else is blocked until prerequisites are met
                _ => PipelineStageStatus.Blocked
            };

            var sortOrder = (int)stageType + 1; // Enum values are ordered correctly
            _pipelineStages.Add(new PipelineStage(Id, stageType, status, sortOrder));
        }
    }
}
