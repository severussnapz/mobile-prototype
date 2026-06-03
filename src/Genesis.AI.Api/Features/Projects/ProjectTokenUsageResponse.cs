using System.Text.Json.Serialization;
using Genesis.AI.Domain.Queries.GetProjectTokenUsage;

namespace Genesis.AI.Api.Features.Projects;

/// <summary>
/// Aggregated token usage and estimated cost for all stages in a project.
/// </summary>
public sealed class ProjectTokenUsageResponse
{
    [JsonPropertyName("stages")]
    public required IReadOnlyList<StageTokenUsageWithCost> Stages { get; init; }

    [JsonPropertyName("totals")]
    public required TokenUsageTotals Totals { get; init; }
}
