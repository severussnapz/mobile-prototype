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

    /// <summary>
    /// Returns the single CSS class selector shared by ALL editable elements in a scope, as ".class".
    /// Returns null when the elements do not collapse to exactly one shared class (genuinely ambiguous).
    /// Domain-agnostic: reasons about how many distinct selectors describe what is present, not meaning.
    /// </summary>
    Task<string?> ResolveConfirmedSelectorForScope(
        Guid projectId,
        string scope,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns every distinct CSS class name present in a fragment scope, with no element cap.
    /// Used by the apply_to_scope guard to decide class existence: the answer is a yes/no set
    /// membership test, so it must see all classes regardless of how deep the element sits.
    /// </summary>
    Task<IReadOnlyCollection<string>> GetClassNamesInScopeAsync(
        Guid projectId,
        string scope,
        CancellationToken cancellationToken);
}
