using System.Text.Json.Serialization;

namespace Genesis.AI.Api.Features.Conversations;

public sealed class PrototypeLockResponse
{
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("appendedDeltaCount")]
    public required int AppendedDeltaCount { get; init; }

    [JsonPropertyName("lockedAt")]
    public required DateTimeOffset LockedAt { get; init; }

    [JsonPropertyName("lockBatchId")]
    public required Guid LockBatchId { get; init; }
}
