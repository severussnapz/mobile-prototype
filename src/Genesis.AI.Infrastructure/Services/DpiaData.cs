using System.Text.Json.Serialization;

namespace Genesis.AI.Infrastructure.Services;

internal sealed class DpiaData
{
    [JsonPropertyName("document_version")]
    public required string DocumentVersion { get; init; }

    [JsonPropertyName("project")]
    public required DpiaProject Project { get; init; }

    [JsonPropertyName("processing")]
    public required DpiaProcessing Processing { get; init; }

    [JsonPropertyName("data_profile")]
    public required DpiaDataProfile DataProfile { get; init; }

    [JsonPropertyName("legal_basis")]
    public required DpiaLegalBasis LegalBasis { get; init; }

    [JsonPropertyName("risk_assessment")]
    public required DpiaRiskAssessment RiskAssessment { get; init; }

    [JsonPropertyName("signoff")]
    public required DpiaSignoff Signoff { get; init; }

    [JsonPropertyName("source_mapping")]
    public List<DpiaSourceMapping> SourceMapping { get; init; } = [];
}
