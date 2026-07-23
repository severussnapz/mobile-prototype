using System.Text.Json;
using Genesis.AI.Api.Features.Projects;
using Genesis.AI.Domain.Queries.GetProjectTokenUsage;

namespace Genesis.AI.Tests.Api.Projects;

public sealed class ProjectTokenUsageResponseSerialiseTests
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    [Fact]
    public void ProjectTokenUsageResponse_SerialisesAllFields()
    {
        // Arrange
        var stageUsage = new StageTokenUsageWithCost(
            StageId: Guid.NewGuid(),
            StageType: "requirements_discovery",
            InputTokens: 120,
            OutputTokens: 45,
            CacheReadInputTokens: 15,
            CacheWriteInputTokens: 8,
            TurnCount: 3,
            EstimatedCost: 1.23m);

        var totals = new TokenUsageTotals
        {
            InputTokens = 200,
            OutputTokens = 75,
            CacheReadInputTokens = 30,
            CacheWriteInputTokens = 10,
            TurnCount = 5,
            EstimatedCost = 2.34m
        };

        var response = new ProjectTokenUsageResponse
        {
            Stages = [stageUsage],
            Totals = totals
        };

        // Act
        var json = JsonSerializer.Serialize(response, JsonOptions);

        // Assert
        var root = JsonDocument.Parse(json).RootElement;

        Assert.True(root.TryGetProperty("stages", out var stagesElement), "stages field missing");
        var stageElement = stagesElement.EnumerateArray().Single();

        Assert.True(stageElement.TryGetProperty("StageId", out var stageIdElement), "StageId field missing");
        Assert.Equal(stageUsage.StageId, stageIdElement.GetGuid());

        Assert.True(stageElement.TryGetProperty("StageType", out var stageTypeElement), "StageType field missing");
        Assert.Equal(stageUsage.StageType, stageTypeElement.GetString());

        Assert.True(stageElement.TryGetProperty("InputTokens", out var inputTokensElement), "InputTokens field missing");
        Assert.Equal(stageUsage.InputTokens, inputTokensElement.GetInt32());

        Assert.True(stageElement.TryGetProperty("OutputTokens", out var outputTokensElement), "OutputTokens field missing");
        Assert.Equal(stageUsage.OutputTokens, outputTokensElement.GetInt32());

        Assert.True(stageElement.TryGetProperty("CacheReadInputTokens", out var cacheReadInputTokensElement), "CacheReadInputTokens field missing");
        Assert.Equal(stageUsage.CacheReadInputTokens, cacheReadInputTokensElement.GetInt32());

        Assert.True(stageElement.TryGetProperty("CacheWriteInputTokens", out var cacheWriteInputTokensElement), "CacheWriteInputTokens field missing");
        Assert.Equal(stageUsage.CacheWriteInputTokens, cacheWriteInputTokensElement.GetInt32());

        Assert.True(stageElement.TryGetProperty("TurnCount", out var turnCountElement), "TurnCount field missing");
        Assert.Equal(stageUsage.TurnCount, turnCountElement.GetInt32());

        Assert.True(stageElement.TryGetProperty("EstimatedCost", out var estimatedCostElement), "EstimatedCost field missing");
        Assert.Equal(stageUsage.EstimatedCost, estimatedCostElement.GetDecimal());

        Assert.True(root.TryGetProperty("totals", out var totalsElement), "totals field missing");

        Assert.True(totalsElement.TryGetProperty("inputTokens", out var totalsInputTokensElement), "inputTokens field missing");
        Assert.Equal(totals.InputTokens, totalsInputTokensElement.GetInt32());

        Assert.True(totalsElement.TryGetProperty("outputTokens", out var totalsOutputTokensElement), "outputTokens field missing");
        Assert.Equal(totals.OutputTokens, totalsOutputTokensElement.GetInt32());

        Assert.True(totalsElement.TryGetProperty("cacheReadInputTokens", out var totalsCacheReadInputTokensElement), "cacheReadInputTokens field missing");
        Assert.Equal(totals.CacheReadInputTokens, totalsCacheReadInputTokensElement.GetInt32());

        Assert.True(totalsElement.TryGetProperty("cacheWriteInputTokens", out var totalsCacheWriteInputTokensElement), "cacheWriteInputTokens field missing");
        Assert.Equal(totals.CacheWriteInputTokens, totalsCacheWriteInputTokensElement.GetInt32());

        Assert.True(totalsElement.TryGetProperty("turnCount", out var totalsTurnCountElement), "turnCount field missing");
        Assert.Equal(totals.TurnCount, totalsTurnCountElement.GetInt32());

        Assert.True(totalsElement.TryGetProperty("estimatedCost", out var totalsEstimatedCostElement), "estimatedCost field missing");
        Assert.Equal(totals.EstimatedCost, totalsEstimatedCostElement.GetDecimal());
    }
}
