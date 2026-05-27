using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Queries.GetProjectTokenUsage;

public class GetProjectTokenUsageQueryHandler : IRequestHandler<GetProjectTokenUsageQuery, ProjectTokenUsageResult>
{
    // Claude Sonnet 4 pricing (per 1M tokens)
    private const decimal InputPricePerMillion = 3.00m;
    private const decimal OutputPricePerMillion = 15.00m;
    private const decimal CacheReadPricePerMillion = 0.30m;
    private const decimal CacheWritePricePerMillion = 3.75m;

    private readonly IConversationRepository _conversationRepository;

    public GetProjectTokenUsageQueryHandler(IConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
    }

    public async Task<ProjectTokenUsageResult> Handle(GetProjectTokenUsageQuery request, CancellationToken cancellationToken)
    {
        var stageUsage = await _conversationRepository.GetTokenUsageByProjectIdAsync(request.ProjectId, cancellationToken);

        var stages = stageUsage.Select(stage =>
        {
            var cost = CalculateCost(stage.InputTokens, stage.OutputTokens, stage.CacheReadInputTokens, stage.CacheWriteInputTokens);
            return new StageTokenUsageWithCost(
                stage.StageId,
                stage.StageType.ToString(),
                stage.InputTokens,
                stage.OutputTokens,
                stage.CacheReadInputTokens,
                stage.CacheWriteInputTokens,
                stage.TurnCount,
                cost);
        }).ToList();

        return new ProjectTokenUsageResult(
            stages,
            stages.Sum(stage => stage.InputTokens),
            stages.Sum(stage => stage.OutputTokens),
            stages.Sum(stage => stage.CacheReadInputTokens),
            stages.Sum(stage => stage.CacheWriteInputTokens),
            stages.Sum(stage => stage.TurnCount),
            stages.Sum(stage => stage.EstimatedCost));
    }

    private static decimal CalculateCost(int inputTokens, int outputTokens, int cacheReadTokens, int cacheWriteTokens)
    {
        var inputCost = inputTokens / 1_000_000m * InputPricePerMillion;
        var outputCost = outputTokens / 1_000_000m * OutputPricePerMillion;
        var cacheReadCost = cacheReadTokens / 1_000_000m * CacheReadPricePerMillion;
        var cacheWriteCost = cacheWriteTokens / 1_000_000m * CacheWritePricePerMillion;
        return Math.Round(inputCost + outputCost + cacheReadCost + cacheWriteCost, 4);
    }
}
