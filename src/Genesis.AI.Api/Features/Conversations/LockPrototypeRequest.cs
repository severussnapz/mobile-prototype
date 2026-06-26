using System.Text.Json.Serialization;

namespace Genesis.AI.Api.Features.Conversations;

public sealed class LockPrototypeRequest
{
    [JsonPropertyName("requirementId")]
    public string? RequirementId { get; init; }

    [JsonPropertyName("requirementFilePath")]
    public string? RequirementFilePath { get; init; }
}
