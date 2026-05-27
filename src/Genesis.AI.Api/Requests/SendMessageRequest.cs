using System.Text.Json.Serialization;

namespace Genesis.AI.Api.Requests;

public sealed class SendMessageRequest
{
    [JsonPropertyName("content")]
    public string Content { get; init; } = null!;
}
