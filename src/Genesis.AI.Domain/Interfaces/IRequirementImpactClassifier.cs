using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Domain.Interfaces;

public interface IRequirementImpactClassifier
{
    Task<RequirementImpact> ClassifyAsync(
        string? userRequest,
        string beforeSummary,
        string afterSummary,
        CancellationToken cancellationToken);
}
