namespace Genesis.AI.Api.Features.Normalisation;

public sealed class NormalisationStatusResponse
{
    public string RunStatus { get; init; } = "not-run";
    public DateTimeOffset? LastRunAtUtc { get; init; }
    public IReadOnlyList<string> RunErrors { get; init; } = [];
    public bool GatePassed { get; init; }
    public bool PlanningEligible { get; init; }
    public bool BypassActive { get; init; }
    public string? BypassedBy { get; init; }
    public DateTimeOffset? BypassedAtUtc { get; init; }
    public IReadOnlyList<string> GateErrors { get; init; } = [];
    public IReadOnlyList<NormalisationArtefactResponse> OutputArtefacts { get; init; } = [];
}
