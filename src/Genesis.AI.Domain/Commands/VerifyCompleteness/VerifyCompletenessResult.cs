using Genesis.AI.Domain.Normalisation;

namespace Genesis.AI.Domain.Commands.VerifyCompleteness;

public sealed record VerifyCompletenessResult(
    VerifyCompletenessStatus Status,
    bool GatePassed,
    IReadOnlyList<string> Errors,
    IReadOnlyList<NormalisationArtefactSummary> OutputArtefacts,
    string? ErrorDetail)
{
    public static VerifyCompletenessResult Failure(VerifyCompletenessStatus status, string errorDetail)
    {
        return new VerifyCompletenessResult(status, false, [], [], errorDetail);
    }
}
