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

    [JsonPropertyName("figmaPatConfigured")]
    public bool FigmaPatConfigured { get; init; }

    [JsonPropertyName("gitHubApiRepoUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GitHubApiRepoUrl { get; init; }

    [JsonPropertyName("gitHubAppRepoUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GitHubAppRepoUrl { get; init; }

    [JsonPropertyName("releaseType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReleaseType { get; init; }

    [JsonPropertyName("assuranceRequired")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? AssuranceRequired { get; init; }

    [JsonPropertyName("pilotDeploymentProcess")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PilotDeploymentProcess { get; init; }

    [JsonPropertyName("csoRoleAssigned")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? CsoRoleAssigned { get; init; }

    [JsonPropertyName("igOwnerRoleAssigned")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IgOwnerRoleAssigned { get; init; }

    [JsonPropertyName("securityReviewerAssigned")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? SecurityReviewerAssigned { get; init; }

    [JsonPropertyName("figmaFileUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FigmaFileUrl { get; init; }

    [JsonPropertyName("figmaPatHint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FigmaPatHint { get; init; }

    [JsonPropertyName("medicalDeviceFlag")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? MedicalDeviceFlag { get; init; }

    [JsonPropertyName("pipelineStages")]
    public IReadOnlyList<PipelineStageResource> PipelineStages { get; init; } = [];
}
