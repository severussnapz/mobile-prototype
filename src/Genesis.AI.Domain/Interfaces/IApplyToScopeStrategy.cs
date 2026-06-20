namespace Genesis.AI.Domain.Interfaces;

public interface IApplyToScopeStrategy
{
    Task<IReadOnlyList<ApplyToScopeValueResult>> DeriveValuesAsync(
        IReadOnlyList<PrototypeDomSearchMatch> matches,
        string? literalValue,
        CancellationToken cancellationToken);
}

public sealed record ApplyToScopeValueResult(
    string NodeKey,
    string FragmentPath,
    string Value);
