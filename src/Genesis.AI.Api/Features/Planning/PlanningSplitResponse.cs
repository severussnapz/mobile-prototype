namespace Genesis.AI.Api.Features.Planning;

public sealed class PlanningSplitResponse
{
    public int TaskCount { get; init; }
    public IReadOnlyList<string> DuplicateTaskIds { get; init; } = [];
    public IReadOnlyList<string> DuplicateCheckAssignments { get; init; } = [];
    public IReadOnlyList<PlanningArtefactResponse> OutputArtefacts { get; init; } = [];
}
