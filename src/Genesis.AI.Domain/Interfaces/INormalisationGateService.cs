using Genesis.AI.Domain.Normalisation;

namespace Genesis.AI.Domain.Interfaces;

public interface INormalisationGateService
{
    Task<NormalisationGateEvaluation> EvaluateAsync(
        Guid projectId,
        string projectCode,
        CancellationToken cancellationToken);
}
