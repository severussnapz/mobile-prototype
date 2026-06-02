using System.Text.Json.Serialization;

namespace Genesis.AI.Api.Features.Conversations;

/// <summary>
/// Identifier of a newly created message.
/// </summary>
public sealed class MessageCreatedResponse
{
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }
}
