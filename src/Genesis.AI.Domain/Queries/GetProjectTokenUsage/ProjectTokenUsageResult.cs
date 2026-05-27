namespace Genesis.AI.Domain.Queries.GetProjectTokenUsage;

public record ProjectTokenUsageResult(
    IReadOnlyList<StageTokenUsageWithCost> Stages,
    int TotalInputTokens,
    int TotalOutputTokens,
    int TotalCacheReadInputTokens,
    int TotalCacheWriteInputTokens,
    int TotalTurnCount,
    decimal TotalEstimatedCost);
