using System.Text.Json.Serialization;

namespace Genesis.AI.Api.Features.Conversations;

public sealed class CreateConversationRequest
{
    [JsonPropertyName("stageId")]
    public Guid StageId { get; init; }
}
