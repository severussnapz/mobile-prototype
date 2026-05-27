using System.Text.Json.Serialization;

namespace Genesis.AI.Api.Requests;

public sealed class CreateConversationRequest
{
    [JsonPropertyName("stageId")]
    public Guid StageId { get; init; }
}
