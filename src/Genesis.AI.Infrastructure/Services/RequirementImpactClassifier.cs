using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

public sealed class RequirementImpactClassifier : IRequirementImpactClassifier
{
    private static readonly AiSystemPrompt ClassifierPrompt = new(
        "Classify whether a prototype UI change alters requirement behaviour. " +
        "Return exactly one token: cosmetic or substantive. " +
        "cosmetic = visual polish/copy tone/spacing/colour without changing task flow or feature capability. " +
        "substantive = changes to workflow, decision paths, data captured/displayed, or how a task is completed.",
        string.Empty);

    private readonly IAiService _aiService;

    public RequirementImpactClassifier(IAiService aiService)
    {
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
    }

    public async Task<RequirementImpact> ClassifyAsync(
        string? userRequest,
        string beforeSummary,
        string afterSummary,
        CancellationToken cancellationToken)
    {
        try
        {
            var payload =
                $"user_request: {userRequest ?? "<none>"}\n" +
                $"before_summary: {beforeSummary}\n" +
                $"after_summary: {afterSummary}\n";

            var response = await _aiService.GenerateResponseAsync(
                ClassifierPrompt,
                [new AiMessage(MessageRole.User, payload)],
                cancellationToken);

            var verdict = response.Content.Trim().ToLowerInvariant();
            if (verdict.Contains("cosmetic", StringComparison.Ordinal))
            {
                return RequirementImpact.Cosmetic;
            }

            if (verdict.Contains("substantive", StringComparison.Ordinal))
            {
                return RequirementImpact.Substantive;
            }
        }
        catch
        {
            // Fall through to fail-safe default.
        }

        // Fail-safe default: uncertain classification is treated as substantive,
        // so potential requirement deltas are surfaced at lock time instead of dropped.
        return RequirementImpact.Substantive;
    }
}
