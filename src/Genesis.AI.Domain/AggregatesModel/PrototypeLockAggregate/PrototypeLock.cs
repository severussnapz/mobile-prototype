using Genesis.AI.Core.Domain;

namespace Genesis.AI.Domain.AggregatesModel.PrototypeLockAggregate;

public class PrototypeLock : Entity, IAggregateRoot
{
    public Guid ProjectId { get; private set; }
    public Guid StageId { get; private set; }
    public DateTimeOffset? LockedAt { get; private set; }
    public string? LockedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private PrototypeLock() { }

    public PrototypeLock(Guid projectId, Guid stageId, TimeProvider timeProvider)
    {
        Id = Guid.NewGuid();
        ProjectId = projectId;
        StageId = stageId;
        UpdatedAt = timeProvider.GetUtcNow();
    }

    public void MarkLocked(DateTimeOffset lockedAt, string lockedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lockedBy);

        LockedAt = lockedAt;
        LockedBy = lockedBy;
        UpdatedAt = lockedAt;
    }

    public void ClearLock(TimeProvider timeProvider)
    {
        LockedAt = null;
        LockedBy = null;
        UpdatedAt = timeProvider.GetUtcNow();
    }
}
