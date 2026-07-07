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
    public string? GitHubApiRepoUrl { get; private set; }
    public string? GitHubAppRepoUrl { get; private set; }
    public string? GitHubRepoOwner { get; private set; }
    public string? GitHubRepoName { get; private set; }
    public string? GitHubInstallationId { get; private set; }
    public string? FigmaFileUrl { get; private set; }
    public string? FigmaPatEncrypted { get; private set; }
    public string? ReleaseType { get; private set; }
    public bool? AssuranceRequired { get; private set; }
    public string? PilotDeploymentProcess { get; private set; }
    public bool? CsoRoleAssigned { get; private set; }
    public bool? IgOwnerRoleAssigned { get; private set; }
    public bool? SecurityReviewerAssigned { get; private set; }
    public bool? MedicalDeviceFlag { get; private set; }

    public bool HasGitHubConfig => GitHubInstallationId is not null;

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

    public void SetGitHubConfig(
        string apiRepoUrl,
        string appRepoUrl,
        string owner,
        string name,
        string installationId,
        TimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiRepoUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(appRepoUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(installationId);
        ArgumentNullException.ThrowIfNull(timeProvider);

        GitHubApiRepoUrl = apiRepoUrl;
        GitHubAppRepoUrl = appRepoUrl;
        GitHubRepoOwner = owner;
        GitHubRepoName = name;
        GitHubInstallationId = installationId;
        UpdatedAt = timeProvider.GetUtcNow();
    }

    public void UpdateDetails(
        string name,
        string? description,
        string timeSheetCode,
        ComplianceDomain complianceDomain,
        TimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(timeSheetCode);
        ArgumentNullException.ThrowIfNull(timeProvider);

        Name = name;
        Description = description;
        TimeSheetCode = timeSheetCode;
        ComplianceDomain = complianceDomain;
        UpdatedAt = timeProvider.GetUtcNow();
    }

    public void UpdateP00Configuration(
        string? releaseType,
        bool? assuranceRequired,
        string? pilotDeploymentProcess,
        bool? csoRoleAssigned,
        bool? igOwnerRoleAssigned,
        bool? securityReviewerAssigned,
        bool? medicalDeviceFlag,
        string? figmaFileUrl,
        string? figmaPatEncrypted,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        ReleaseType = releaseType;
        AssuranceRequired = assuranceRequired;
        PilotDeploymentProcess = pilotDeploymentProcess;
        CsoRoleAssigned = csoRoleAssigned;
        IgOwnerRoleAssigned = igOwnerRoleAssigned;
        SecurityReviewerAssigned = securityReviewerAssigned;
        MedicalDeviceFlag = medicalDeviceFlag;
        FigmaFileUrl = figmaFileUrl;
        FigmaPatEncrypted = figmaPatEncrypted;
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
