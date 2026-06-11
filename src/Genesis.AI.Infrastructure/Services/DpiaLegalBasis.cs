using System.Text.Json.Serialization;

namespace Genesis.AI.Infrastructure.Services;

internal sealed class DpiaLegalBasis
{
    [JsonPropertyName("article6")]
    public required string Article6 { get; init; }

    [JsonPropertyName("article9")]
    public string? Article9 { get; init; }

    [JsonPropertyName("lawful_purpose")]
    public required string LawfulPurpose { get; init; }

    [JsonPropertyName("privacy_notice_reference")]
    public required string PrivacyNoticeReference { get; init; }
}
