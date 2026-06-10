namespace Genesis.AI.Api.Features.Planning;

public sealed class PlanningRunActionResponse
{
    public bool PreflightPassed { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public IReadOnlyList<PlanningArtefactResponse> OutputArtefacts { get; init; } = [];
}
