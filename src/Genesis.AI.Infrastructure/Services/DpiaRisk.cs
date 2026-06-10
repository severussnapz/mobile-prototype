using System.Text.Json.Serialization;

namespace Genesis.AI.Infrastructure.Services;

internal sealed class DpiaRisk
{
    [JsonPropertyName("risk_id")]
    public required string RiskId { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("likelihood")]
    public required string Likelihood { get; init; }

    [JsonPropertyName("impact")]
    public required string Impact { get; init; }

    [JsonPropertyName("controls")]
    public List<string> Controls { get; init; } = [];

    [JsonPropertyName("residual_risk")]
    public required string ResidualRisk { get; init; }

    [JsonPropertyName("check_ids")]
    public List<string> CheckIds { get; init; } = [];
}
