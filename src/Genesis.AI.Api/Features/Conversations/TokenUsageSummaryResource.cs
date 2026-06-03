using System.Text.Json.Serialization;

namespace Genesis.AI.Api.Features.Conversations;

public sealed class TokenUsageSummaryResource
{
    [JsonPropertyName("totalInputTokens")]
    public int TotalInputTokens { get; init; }

    [JsonPropertyName("totalOutputTokens")]
    public int TotalOutputTokens { get; init; }

    [JsonPropertyName("totalCacheReadTokens")]
    public int TotalCacheReadTokens { get; init; }

    [JsonPropertyName("totalCacheWriteTokens")]
    public int TotalCacheWriteTokens { get; init; }

    [JsonPropertyName("turnCount")]
    public int TurnCount { get; init; }
}
