namespace Genesis.AI.Domain.Interfaces;

public sealed record ApplyToScopeValueResult(
    string NodeKey,
    string FragmentPath,
    string Value);
