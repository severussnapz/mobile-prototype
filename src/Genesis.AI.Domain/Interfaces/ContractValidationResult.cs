namespace Genesis.AI.Domain.Interfaces;

public sealed record ContractValidationResult(
    bool IsValid,
    IReadOnlyList<string> Violations);
