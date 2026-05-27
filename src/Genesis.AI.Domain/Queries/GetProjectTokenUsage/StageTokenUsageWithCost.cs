namespace Genesis.AI.Domain.Queries.GetProjectTokenUsage;

public record StageTokenUsageWithCost(
    Guid StageId,
    string StageType,
    int InputTokens,
    int OutputTokens,
    int CacheReadInputTokens,
    int CacheWriteInputTokens,
    int TurnCount,
    decimal EstimatedCost);
