using System.Text.Json.Serialization;

namespace Genesis.AI.Api.Http;

/// <summary>
/// A single error entry within an <see cref="ApiErrorResponse"/>.
/// </summary>
public sealed class ApiError
{
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; init; }

    [JsonPropertyName("detail")]
    public required string Detail { get; init; }
}
