namespace Genesis.AI.Domain.Interfaces;

public sealed record PrototypeLockResult(
    bool Success,
    string Message,
    int AppendedDeltaCount,
    DateTimeOffset LockedAt,
    Guid LockBatchId);
