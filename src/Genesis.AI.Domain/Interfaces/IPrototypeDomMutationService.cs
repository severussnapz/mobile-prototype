namespace Genesis.AI.Domain.Interfaces;

/// <summary>
/// Applies targeted DOM mutations to prototype fragments.
/// Phase 0 contract only: implementation wiring is added in later phases.
/// </summary>
public interface IPrototypeDomMutationService
{
    Task<PrototypeDomMutationResult> ApplyMutationAsync(
        PrototypeDomMutationRequest request,
        CancellationToken cancellationToken);

    Task<PrototypeDomBatchMutationResult> ApplyBatchMutationAsync(
        IReadOnlyList<PrototypeDomMutationRequest> requests,
        CancellationToken cancellationToken);
}

public sealed record PrototypeDomMutationRequest(
    Guid ProjectId,
    string FragmentPath,
    string NodeKey,
    PrototypeDomMutationOperation Operation,
    string? Attribute,
    string? Value,
    string CreatedBy);

public enum PrototypeDomMutationOperation
{
    SetAttribute = 1,
    SetText = 2,
    AddClass = 3,
    RemoveClass = 4,
    InsertAdjacentHtml = 5,
    RemoveElement = 6
}

public sealed record PrototypeDomMutationResult(
    bool Success,
    string Message,
    string? FragmentPath,
    int? Version);

public sealed record PrototypeDomBatchMutationItemResult(
    string NodeKey,
    bool Success,
    string Message,
    string? FragmentPath,
    int? Version);

public sealed record PrototypeDomMutationFragmentResult(
    Guid ProjectId,
    string FragmentPath,
    int Version);

public sealed record PrototypeDomBatchMutationResult(
    int TotalMutations,
    int SuccessfulMutations,
    IReadOnlyList<PrototypeDomBatchMutationItemResult> Results,
    IReadOnlyList<PrototypeDomMutationFragmentResult> PersistedFragments);
