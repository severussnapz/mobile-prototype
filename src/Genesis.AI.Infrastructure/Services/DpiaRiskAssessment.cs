using System.Text.Json.Serialization;

namespace Genesis.AI.Infrastructure.Services;

internal sealed class DpiaRiskAssessment
{
    [JsonPropertyName("risks")]
    public List<DpiaRisk> Risks { get; init; } = [];
}
