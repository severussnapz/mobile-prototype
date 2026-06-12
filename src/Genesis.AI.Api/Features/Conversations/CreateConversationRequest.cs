using System.Text.Json.Serialization;

namespace Genesis.AI.Api.Features.Conversations;

public sealed class CreateConversationRequest
{
    [JsonPropertyName("stageId")]
    public Guid StageId { get; init; }

    /// <summary>
    /// Optional requirement identifier (e.g. "REQ-001") to scope this conversation to a
    /// specific requirement. When provided, enables per-requirement windowing — one
    /// conversation per requirement per stage so each has an independent message window.
    /// Null for non-windowed stages (P1, P2, P9, P10) and legacy conversations.
    /// </summary>
    [JsonPropertyName("requirementId")]
    public string? RequirementId { get; init; }
}
