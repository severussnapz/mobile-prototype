namespace Genesis.AI.Domain.Interfaces;

public sealed record PrototypeDomListRequest(
    Guid ProjectId,
    string Selector,
    string Scope,
    string CreatedBy);
