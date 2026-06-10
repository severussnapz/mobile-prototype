using System.Text.Json.Serialization;

namespace Genesis.AI.Infrastructure.Services;

internal sealed class DpiaSignoff
{
    [JsonPropertyName("ig_reviewer")]
    public required string IgReviewer { get; init; }

    [JsonPropertyName("role")]
    public required string Role { get; init; }

    [JsonPropertyName("decision")]
    public required string Decision { get; init; }

    [JsonPropertyName("reference")]
    public required string Reference { get; init; }

    [JsonPropertyName("date")]
    public required string Date { get; init; }
}
