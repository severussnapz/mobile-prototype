namespace Genesis.AI.Domain.Interfaces;

public sealed record PrototypeDomBatchMutationResult(
    int TotalMutations,
    int SuccessfulMutations,
    IReadOnlyList<PrototypeDomBatchMutationItemResult> Results,
    IReadOnlyList<PrototypeDomMutationFragmentResult> PersistedFragments);
