namespace Genesis.AI.Domain.Interfaces;

/// <summary>
/// Emitted when the AI stream completes with token usage metadata.
/// </summary>
public record AiTokenUsage(int InputTokens, int OutputTokens, int TotalTokens, int CacheReadInputTokens, int CacheWriteInputTokens) : AiStreamEvent;
