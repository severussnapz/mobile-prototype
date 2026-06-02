using System.Text.Json.Serialization;

namespace Genesis.AI.Api.Features.Stages;

/// <summary>
/// An informational message about a stage, such as a no-op acknowledgement.
/// </summary>
public sealed class StageMessageResponse
{
    [JsonPropertyName("stageId")]
    public required Guid StageId { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }
}
