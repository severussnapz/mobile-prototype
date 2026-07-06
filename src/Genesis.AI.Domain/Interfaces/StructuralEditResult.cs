namespace Genesis.AI.Domain.Interfaces;

public sealed record StructuralEditResult(
    bool Success,
    string Message,
    IReadOnlyList<string> AffectedPaths);
