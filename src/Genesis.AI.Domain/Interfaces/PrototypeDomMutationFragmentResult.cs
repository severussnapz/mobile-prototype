namespace Genesis.AI.Domain.Interfaces;

public sealed record PrototypeDomMutationFragmentResult(
    Guid ProjectId,
    string FragmentPath,
    int Version);
