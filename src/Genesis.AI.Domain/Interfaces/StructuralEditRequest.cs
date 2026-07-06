namespace Genesis.AI.Domain.Interfaces;

public sealed record StructuralEditRequest(
    string Operation,
    string? FragmentPath,
    IReadOnlyList<string>? OrderedFragmentPaths,
    bool? Hidden);
