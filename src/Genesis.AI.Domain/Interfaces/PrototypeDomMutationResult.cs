namespace Genesis.AI.Domain.Interfaces;

public sealed record PrototypeDomMutationResult(
    bool Success,
    string Message,
    string? FragmentPath,
    int? Version);
