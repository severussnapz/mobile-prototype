using System.Text.Json.Serialization;

namespace Genesis.AI.Infrastructure.Services;

internal sealed class DpiaProject
{
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("request_date")]
    public required string RequestDate { get; init; }

    [JsonPropertyName("contact_name")]
    public required string ContactName { get; init; }

    [JsonPropertyName("sponsor")]
    public required string Sponsor { get; init; }

    [JsonPropertyName("business_unit")]
    public required string BusinessUnit { get; init; }

    [JsonPropertyName("proposition")]
    public required string Proposition { get; init; }

    [JsonPropertyName("environment")]
    public required string Environment { get; init; }

    [JsonPropertyName("stakeholders")]
    public List<string> Stakeholders { get; init; } = [];

    [JsonPropertyName("change_type")]
    public required string ChangeType { get; init; }

    [JsonPropertyName("summary")]
    public required string Summary { get; init; }

    [JsonPropertyName("data_flow")]
    public required string DataFlow { get; init; }
}
