namespace Genesis.AI.Domain.Interfaces;

public sealed record PrototypeDomSearchResult(
    IReadOnlyList<PrototypeDomSearchMatch> Matches,
    bool Truncated,
    int TotalMatches);
