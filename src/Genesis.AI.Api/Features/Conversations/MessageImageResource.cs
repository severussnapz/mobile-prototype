using System.Text.Json.Serialization;

namespace Genesis.AI.Api.Features.Conversations;

public sealed class MessageImageResource
{
    [JsonPropertyName("data")]
    public string Data { get; init; } = null!;

    [JsonPropertyName("mediaType")]
    public string MediaType { get; init; } = null!;
}
