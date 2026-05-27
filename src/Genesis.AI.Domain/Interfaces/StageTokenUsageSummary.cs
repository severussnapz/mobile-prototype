using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Domain.Interfaces;

public record StageTokenUsageSummary(
    Guid StageId,
    StageType StageType,
    int InputTokens,
    int OutputTokens,
    int CacheReadInputTokens,
    int CacheWriteInputTokens,
    int TurnCount);
