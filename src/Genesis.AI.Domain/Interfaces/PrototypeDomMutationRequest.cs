namespace Genesis.AI.Domain.Interfaces;

public sealed record PrototypeDomMutationRequest(
    Guid ProjectId,
    string FragmentPath,
    string NodeKey,
    PrototypeDomMutationOperation Operation,
    string? Attribute,
    string? Value,
    string CreatedBy);
