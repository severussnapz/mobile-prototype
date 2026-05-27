using System.Text.Json.Serialization;

namespace Genesis.AI.Api.Resources;

public sealed class MessageResource
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("role")]
    public string Role { get; init; } = null!;

    [JsonPropertyName("content")]
    public string Content { get; init; } = null!;

    [JsonPropertyName("tokenCount")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TokenCount { get; init; }

    [JsonPropertyName("givenName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GivenName { get; init; }

    [JsonPropertyName("familyName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FamilyName { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }
}
