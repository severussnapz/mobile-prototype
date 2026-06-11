using System.Text.Json.Serialization;

namespace Genesis.AI.Infrastructure.Services;

internal sealed class DpiaProcessing
{
    [JsonPropertyName("personal_data")]
    public bool PersonalData { get; init; }

    [JsonPropertyName("special_category_data")]
    public bool SpecialCategoryData { get; init; }

    [JsonPropertyName("minors_data")]
    public bool MinorsData { get; init; }

    [JsonPropertyName("volume")]
    public required string Volume { get; init; }

    [JsonPropertyName("frequency")]
    public required string Frequency { get; init; }

    [JsonPropertyName("role")]
    public required string Role { get; init; }

    [JsonPropertyName("data_controller")]
    public required string DataController { get; init; }

    [JsonPropertyName("data_subjects")]
    public List<string> DataSubjects { get; init; } = [];

    [JsonPropertyName("recipients")]
    public List<string> Recipients { get; init; } = [];

    [JsonPropertyName("third_parties")]
    public List<string> ThirdParties { get; init; } = [];
}
