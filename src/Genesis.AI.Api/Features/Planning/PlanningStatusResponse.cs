namespace Genesis.AI.Api.Features.Planning;

public sealed class PlanningStatusResponse
{
    public bool PreflightPassed { get; init; }
    public DateTimeOffset? LastPreflightAtUtc { get; init; }
    public IReadOnlyList<string> PreflightErrors { get; init; } = [];
    public bool TaskPlanExists { get; init; }
    public bool TasksDataExists { get; init; }
    public bool EmApproved { get; init; }
    public bool EmApprovalIsStale { get; init; }
    public string? ApprovedBy { get; init; }
    public DateTimeOffset? ApprovedAtUtc { get; init; }
    public bool SplitPassed { get; init; }
    public int TaskCount { get; init; }
    public bool GatePassed { get; init; }
    public IReadOnlyList<string> GateErrors { get; init; } = [];
    public IReadOnlyList<PlanningArtefactResponse> OutputArtefacts { get; init; } = [];
}
