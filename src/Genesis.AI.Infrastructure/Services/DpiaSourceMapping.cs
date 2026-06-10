using System.Text.Json.Serialization;

namespace Genesis.AI.Infrastructure.Services;

internal sealed class DpiaSourceMapping
{
    [JsonPropertyName("control_id")]
    public required string ControlId { get; init; }

    [JsonPropertyName("source_document")]
    public required string SourceDocument { get; init; }

    [JsonPropertyName("source_section")]
    public required string SourceSection { get; init; }
}
