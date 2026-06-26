namespace Genesis.AI.Domain.Interfaces;

public interface IRequirementsFeedbackLoopService
{
    Task RecordUiDeltaAsync(UiDeltaRecordRequest request, CancellationToken cancellationToken);

    Task<PrototypeLockResult> LockPrototypeAsync(
        Guid projectId,
        string requirementId,
        string requirementFilePath,
        string lockedBy,
        CancellationToken cancellationToken);
}
