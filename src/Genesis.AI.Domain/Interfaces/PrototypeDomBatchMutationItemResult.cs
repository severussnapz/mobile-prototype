namespace Genesis.AI.Domain.Interfaces;

public sealed record PrototypeDomBatchMutationItemResult(
    string NodeKey,
    bool Success,
    string Message,
    string? FragmentPath,
    int? Version);
