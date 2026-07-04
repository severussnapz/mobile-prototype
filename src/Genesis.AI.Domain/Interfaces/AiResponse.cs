namespace Genesis.AI.Domain.Interfaces;

public record AiResponse(string Content, int InputTokens, int OutputTokens, int CacheReadInputTokens, int CacheWriteInputTokens);
