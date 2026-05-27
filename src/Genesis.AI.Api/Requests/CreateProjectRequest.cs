using System.Text.Json.Serialization;

namespace Genesis.AI.Api.Requests;

public sealed class CreateProjectRequest
{
    [JsonPropertyName("code")]
    public string Code { get; init; } = null!;

    [JsonPropertyName("name")]
    public string Name { get; init; } = null!;

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("complianceDomain")]
    public string ComplianceDomain { get; init; } = null!;
}
