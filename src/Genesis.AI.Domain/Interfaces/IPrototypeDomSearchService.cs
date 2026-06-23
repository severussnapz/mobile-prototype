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

    /// <summary>
    /// Lists every editable element actually present in a single fragment scope, regardless of selector.
    /// Used when a caller's selector matches zero elements — returns the real elements so the
    /// correct selector is discoverable and a wrong selector can never be silently written.
    /// Scope is a fragment filename without path or extension (e.g. "_shell", "screen-01-legacy").
    /// </summary>
    Task<PrototypeDomSearchResult> ListAllInScopeAsync(
        Guid projectId,
        string scope,
        CancellationToken cancellationToken);
}
