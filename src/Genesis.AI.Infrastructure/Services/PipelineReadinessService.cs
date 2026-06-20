using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

public sealed class PipelineReadinessService : IPipelineReadinessService
{
    private readonly IRequirementChangeRepository _repository;
    private readonly IContractValidationService _contractValidationService;

    public PipelineReadinessService(
        IRequirementChangeRepository repository,
        IContractValidationService contractValidationService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _contractValidationService = contractValidationService ??
            throw new ArgumentNullException(nameof(contractValidationService));
    }

    public async Task<PipelineReadinessResult> GetReadinessAsync(
        Guid projectId,
        IReadOnlyDictionary<string, string> reqContents,
        CancellationToken cancellationToken)
    {
        var blockers = new List<string>();

        await CheckOpenDefiniteReviewsAsync(projectId, blockers, cancellationToken);
        CheckContractViolations(reqContents, blockers);

        return new PipelineReadinessResult(
            IsReady: blockers.Count == 0,
            Blockers: blockers);
    }

    private async Task CheckOpenDefiniteReviewsAsync(
        Guid projectId,
        List<string> blockers,
        CancellationToken cancellationToken)
    {
        var hasOpenReviews = await _repository.HasOpenDefiniteReviewsAsync(
            projectId, cancellationToken);

        if (!hasOpenReviews)
        {
            return;
        }

        var changes = await _repository.GetByProjectIdAsync(projectId, cancellationToken);

        foreach (var change in changes)
        {
            if (!change.HasOpenDefiniteReviews())
            {
                continue;
            }

            var domains = BuildOpenReviewDomains(change);
            blockers.Add(
                $"CHANGE-{change.Id.ToString("N")[..8].ToUpperInvariant()} ({change.ReqId}): " +
                $"definite {domains} review outstanding — must be signed off before normalisation");
        }
    }

    private static string BuildOpenReviewDomains(RequirementChange change)
    {
        var domains = new List<string>();
        if (change.ClinicalSafetyImpact == ImpactLevel.Definite && !change.ClinicalSafetyReviewed)
        {
            domains.Add("Clinical Safety");
        }

        if (change.IgImpact == ImpactLevel.Definite && !change.IgReviewed)
        {
            domains.Add("IG");
        }

        if (change.SecurityImpact == ImpactLevel.Definite && !change.SecurityReviewed)
        {
            domains.Add("Security");
        }

        return string.Join(", ", domains);
    }

    private void CheckContractViolations(
        IReadOnlyDictionary<string, string> reqContents,
        List<string> blockers)
    {
        foreach (var (reqId, content) in reqContents)
        {
            var result = _contractValidationService.ValidatePipeline01(content);
            foreach (var violation in result.Violations)
            {
                blockers.Add($"{reqId}: {violation}");
            }
        }
    }
}
