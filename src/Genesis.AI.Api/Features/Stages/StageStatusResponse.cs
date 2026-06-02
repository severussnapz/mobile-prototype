using System.Text.Json.Serialization;

namespace Genesis.AI.Api.Features.Stages;

/// <summary>
/// The outcome of a stage state transition.
/// </summary>
public sealed class StageStatusResponse
{
    [JsonPropertyName("stageId")]
    public required Guid? StageId { get; init; }

    [JsonPropertyName("stageType")]
    public required string? StageType { get; init; }

    [JsonPropertyName("status")]
    public required string? Status { get; init; }
}
