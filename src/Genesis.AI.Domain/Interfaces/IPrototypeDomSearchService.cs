namespace Genesis.AI.Domain.Interfaces;

/// <summary>
/// Searches prototype HTML fragments using a DOM parser and returns ranked node matches.
/// Phase 0 contract only: implementation wiring is added in later phases.
/// </summary>
public interface IPrototypeDomSearchService
{
    Task<PrototypeDomSearchResult> SearchAsync(
        PrototypeDomSearchRequest request,
        CancellationToken cancellationToken);

    Task<PrototypeDomSearchResult> ListAllAsync(
        PrototypeDomListRequest request,
        CancellationToken cancellationToken);
}

public sealed record PrototypeDomSearchRequest(
    Guid ProjectId,
    string FilePath,
    string Query,
    string CreatedBy);

public sealed record PrototypeDomListRequest(
    Guid ProjectId,
    string Selector,
    string? ScopeNodeId,
    string CreatedBy);

public sealed record PrototypeDomSearchResult(
    IReadOnlyList<PrototypeDomSearchMatch> Matches,
    bool Truncated,
    int TotalMatches);

public sealed record PrototypeDomSearchMatch(
    string NodeKey,
    string FragmentPath,
    string TagName,
    string TextSnippet,
    string CssSelector,
    IReadOnlyList<string> ClassList,
    string ParentContext,
    string SiblingContext);
