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
