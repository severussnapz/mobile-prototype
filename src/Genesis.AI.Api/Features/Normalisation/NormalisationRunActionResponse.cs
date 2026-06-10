namespace Genesis.AI.Api.Features.Normalisation;

public sealed class NormalisationRunActionResponse
{
    public string Action { get; init; } = "run_local_normaliser";
    public string RunStatus { get; init; } = string.Empty;
    public bool GatePassed { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public IReadOnlyList<NormalisationArtefactResponse> OutputArtefacts { get; init; } = [];
}
