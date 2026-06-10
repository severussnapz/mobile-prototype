using Genesis.AI.Domain.Normalisation;

namespace Genesis.AI.Domain.Commands.RunLocalNormaliser;

public sealed record RunLocalNormaliserResult(
    RunLocalNormaliserStatus Status,
    string RunStatus,
    bool GatePassed,
    IReadOnlyList<string> Errors,
    IReadOnlyList<NormalisationArtefactSummary> OutputArtefacts,
    string? ErrorDetail)
{
    public static RunLocalNormaliserResult Failure(RunLocalNormaliserStatus status, string errorDetail)
    {
        return new RunLocalNormaliserResult(status, "failed", false, [], [], errorDetail);
    }
}
