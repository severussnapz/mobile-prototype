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

    /// <summary>
    /// When set, the new conversation is a continuation of this conversation (e.g. after
    /// hitting the tool-use limit). The stream controller will inject a handover block into
    /// the system prompt so the AI knows where the previous conversation left off.
    /// </summary>
    [JsonPropertyName("continuedFromConversationId")]
    public Guid? ContinuedFromConversationId { get; init; }
}
