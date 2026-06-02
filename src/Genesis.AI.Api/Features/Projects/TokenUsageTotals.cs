using System.Text.Json.Serialization;

namespace Genesis.AI.Api.Features.Projects;

/// <summary>
/// Project-wide token usage totals across all stages.
/// </summary>
public sealed class TokenUsageTotals
{
    [JsonPropertyName("inputTokens")]
    public required int InputTokens { get; init; }

    [JsonPropertyName("outputTokens")]
    public required int OutputTokens { get; init; }

    [JsonPropertyName("cacheReadInputTokens")]
    public required int CacheReadInputTokens { get; init; }

    [JsonPropertyName("cacheWriteInputTokens")]
    public required int CacheWriteInputTokens { get; init; }

    [JsonPropertyName("turnCount")]
    public required int TurnCount { get; init; }

    [JsonPropertyName("estimatedCost")]
    public required decimal EstimatedCost { get; init; }
}
