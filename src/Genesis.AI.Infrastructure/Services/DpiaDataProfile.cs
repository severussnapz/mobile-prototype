using System.Text.Json.Serialization;

namespace Genesis.AI.Infrastructure.Services;

internal sealed class DpiaDataProfile
{
    [JsonPropertyName("classifications")]
    public List<string> Classifications { get; init; } = [];

    [JsonPropertyName("data_categories")]
    public List<string> DataCategories { get; init; } = [];

    [JsonPropertyName("retention_rule")]
    public required string RetentionRule { get; init; }

    [JsonPropertyName("deletion_trigger")]
    public required string DeletionTrigger { get; init; }

    [JsonPropertyName("sharing_methods")]
    public List<string> SharingMethods { get; init; } = [];

    [JsonPropertyName("encryption_at_rest")]
    public required string EncryptionAtRest { get; init; }

    [JsonPropertyName("encryption_in_transit")]
    public required string EncryptionInTransit { get; init; }
}
