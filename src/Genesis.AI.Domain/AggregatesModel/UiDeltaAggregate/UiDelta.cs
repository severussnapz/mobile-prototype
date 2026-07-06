using Genesis.AI.Core.Domain;
using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Domain.AggregatesModel.UiDeltaAggregate;

public class UiDelta : Entity, IAggregateRoot
{
    public Guid ProjectId { get; private set; }
    public Guid StageId { get; private set; }
    public string? RequirementId { get; private set; }
    public string TargetId { get; private set; } = null!;
    public string FilePath { get; private set; } = null!;
    public string OperationType { get; private set; } = null!;
    public string SourceType { get; private set; } = null!;
    public string? UserRequest { get; private set; }
    public string BeforeSummary { get; private set; } = null!;
    public string AfterSummary { get; private set; } = null!;
    public RequirementImpact RequirementImpact { get; private set; }
    public Guid? ConversationId { get; private set; }
    public Guid? MessageId { get; private set; }
    public Guid? LockBatchId { get; private set; }
    public string? LockedRequirementFilePath { get; private set; }
    public DateTimeOffset? LockedAt { get; private set; }
    public string CreatedBy { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private UiDelta() { }

    public UiDelta(
        Guid projectId,
        Guid stageId,
        string? requirementId,
        string targetId,
        string filePath,
        string operationType,
        string sourceType,
        string? userRequest,
        string beforeSummary,
        string afterSummary,
        RequirementImpact requirementImpact,
        string createdBy,
        TimeProvider timeProvider,
        Guid? conversationId = null,
        Guid? messageId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetId);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationType);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceType);
        ArgumentException.ThrowIfNullOrWhiteSpace(beforeSummary);
        ArgumentException.ThrowIfNullOrWhiteSpace(afterSummary);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        Id = Guid.NewGuid();
        ProjectId = projectId;
        StageId = stageId;
        RequirementId = requirementId;
        TargetId = targetId;
        FilePath = filePath;
        OperationType = operationType;
        SourceType = sourceType;
        UserRequest = userRequest;
        BeforeSummary = beforeSummary;
        AfterSummary = afterSummary;
        RequirementImpact = requirementImpact;
        CreatedBy = createdBy;
        CreatedAt = timeProvider.GetUtcNow();
        ConversationId = conversationId;
        MessageId = messageId;
    }

    public bool IsUnlockedSubstantive()
    {
        return RequirementImpact == RequirementImpact.Substantive && LockedAt is null;
    }

    public void MarkLocked(Guid lockBatchId, string requirementFilePath, DateTimeOffset lockedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requirementFilePath);

        LockBatchId = lockBatchId;
        LockedRequirementFilePath = requirementFilePath;
        LockedAt = lockedAt;
    }
}
