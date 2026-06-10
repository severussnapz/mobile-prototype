namespace Genesis.AI.Api.Features.Normalisation;

public sealed class NormalisationVerifyResponse
{
    public bool GatePassed { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public IReadOnlyList<NormalisationArtefactResponse> OutputArtefacts { get; init; } = [];
}
