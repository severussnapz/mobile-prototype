namespace Genesis.AI.Domain.Interfaces;

public sealed record PrototypeDomSearchRequest(
    Guid ProjectId,
    string FilePath,
    string Query,
    string CreatedBy);
