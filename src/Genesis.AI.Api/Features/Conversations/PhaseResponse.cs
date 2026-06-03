using System.Text.Json.Serialization;

namespace Genesis.AI.Api.Features.Conversations;

/// <summary>
/// The current phase of a conversation after a phase transition.
/// </summary>
public sealed class PhaseResponse
{
    [JsonPropertyName("phase")]
    public required int? Phase { get; init; }

    [JsonPropertyName("phaseName")]
    public required string? PhaseName { get; init; }
}
