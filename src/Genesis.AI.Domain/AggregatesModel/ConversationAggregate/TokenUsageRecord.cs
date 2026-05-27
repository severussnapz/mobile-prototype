using Genesis.AI.Core.Domain;

namespace Genesis.AI.Domain.AggregatesModel.ConversationAggregate;

public class TokenUsageRecord : Entity
{
    public Guid ConversationId { get; private set; }
    public int InputTokens { get; private set; }
    public int OutputTokens { get; private set; }
    public int CacheReadInputTokens { get; private set; }
    public int CacheWriteInputTokens { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private TokenUsageRecord() { } // Required for EF Core

    internal TokenUsageRecord(Guid conversationId, int inputTokens, int outputTokens, int cacheReadInputTokens, int cacheWriteInputTokens, TimeProvider timeProvider)
    {
        Id = Guid.NewGuid();
        ConversationId = conversationId;
        InputTokens = inputTokens;
        OutputTokens = outputTokens;
        CacheReadInputTokens = cacheReadInputTokens;
        CacheWriteInputTokens = cacheWriteInputTokens;
        CreatedAt = timeProvider.GetUtcNow();
    }
}
