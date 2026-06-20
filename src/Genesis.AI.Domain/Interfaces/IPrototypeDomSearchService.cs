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
