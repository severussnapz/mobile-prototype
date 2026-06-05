using System.Text.Json.Serialization;

namespace Genesis.AI.Api.Features.Conversations;

public sealed class MessageDocumentResource
{
    [JsonPropertyName("data")]
    public string Data { get; init; } = null!;

    [JsonPropertyName("mediaType")]
    public string MediaType { get; init; } = null!;

    [JsonPropertyName("fileName")]
    public string FileName { get; init; } = null!;
}
