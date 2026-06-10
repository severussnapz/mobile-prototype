using Genesis.AI.Domain.Planning;

namespace Genesis.AI.Domain.Interfaces;

public interface IPlanningGateService
{
    Task<PlanningGateEvaluation> EvaluateAsync(Guid projectId, CancellationToken cancellationToken);
}
