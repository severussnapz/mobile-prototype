using System.Text.Json.Serialization;

namespace Genesis.AI.Api.Features.Projects;

public sealed class ProjectResource
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("code")]
    public string Code { get; init; } = null!;

    [JsonPropertyName("name")]
    public string Name { get; init; } = null!;

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; init; }

    [JsonPropertyName("timeSheetCode")]
    public string TimeSheetCode { get; init; } = null!;

    [JsonPropertyName("complianceDomain")]
    public string ComplianceDomain { get; init; } = null!;

    [JsonPropertyName("status")]
    public string Status { get; init; } = null!;

    [JsonPropertyName("createdBy")]
    public string CreatedBy { get; init; } = null!;

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; init; }

    [JsonPropertyName("pipelineStages")]
    public IReadOnlyList<PipelineStageResource> PipelineStages { get; init; } = [];
}
