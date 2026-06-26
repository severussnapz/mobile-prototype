using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

public sealed class LiteralStrategy : IApplyToScopeStrategy
{
    public Task<IReadOnlyList<ApplyToScopeValueResult>> DeriveValuesAsync(
        IReadOnlyList<PrototypeDomSearchMatch> matches,
        string? literalValue,
        CancellationToken cancellationToken)
    {
        var value = literalValue ?? string.Empty;
        var results = matches
            .Select(match => new ApplyToScopeValueResult(
                NodeKey: match.NodeKey,
                FragmentPath: match.FragmentPath,
                Value: value))
            .ToList();

        return Task.FromResult<IReadOnlyList<ApplyToScopeValueResult>>(results);
    }
}
