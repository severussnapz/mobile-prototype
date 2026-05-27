using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Domain.Queries.GetProjectTokenUsage;
using Moq;

namespace Genesis.AI.Tests.Queries;

public class GetProjectTokenUsageQueryHandlerTests
{
    private readonly Mock<IConversationRepository> _conversationRepositoryMock;
    private readonly GetProjectTokenUsageQueryHandler _handler;

    public GetProjectTokenUsageQueryHandlerTests()
    {
        _conversationRepositoryMock = new Mock<IConversationRepository>();
        _handler = new GetProjectTokenUsageQueryHandler(_conversationRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithUsageData_ReturnsStageUsageWithCost()
    {
        var projectId = Guid.NewGuid();
        var stageId = Guid.NewGuid();
        var expectedUsage = new List<StageTokenUsageSummary>
        {
            new(stageId, StageType.RequirementsDiscovery, 5000, 12000, 2000, 500, 8)
        };

        _conversationRepositoryMock
            .Setup(repository => repository.GetTokenUsageByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUsage);

        var query = new GetProjectTokenUsageQuery(projectId);

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Single(result.Stages);
        var stage = result.Stages[0];
        Assert.Equal(stageId, stage.StageId);
        Assert.Equal("RequirementsDiscovery", stage.StageType);
        Assert.Equal(5000, stage.InputTokens);
        Assert.Equal(12000, stage.OutputTokens);
        Assert.Equal(2000, stage.CacheReadInputTokens);
        Assert.Equal(500, stage.CacheWriteInputTokens);
        Assert.Equal(8, stage.TurnCount);
        Assert.True(stage.EstimatedCost > 0);
    }

    [Fact]
    public async Task Handle_WithNoUsageData_ReturnsEmptyResult()
    {
        var projectId = Guid.NewGuid();
        _conversationRepositoryMock
            .Setup(repository => repository.GetTokenUsageByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StageTokenUsageSummary>());

        var query = new GetProjectTokenUsageQuery(projectId);

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Empty(result.Stages);
        Assert.Equal(0, result.TotalInputTokens);
        Assert.Equal(0, result.TotalEstimatedCost);
    }

    [Fact]
    public async Task Handle_WithMultipleStages_ReturnsTotalsAcrossAllStages()
    {
        var projectId = Guid.NewGuid();
        var expectedUsage = new List<StageTokenUsageSummary>
        {
            new(Guid.NewGuid(), StageType.RequirementsDiscovery, 5000, 12000, 2000, 500, 8),
            new(Guid.NewGuid(), StageType.Architecture, 3000, 8000, 1000, 250, 5),
            new(Guid.NewGuid(), StageType.Design, 2000, 6000, 500, 100, 3),
        };

        _conversationRepositoryMock
            .Setup(repository => repository.GetTokenUsageByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUsage);

        var query = new GetProjectTokenUsageQuery(projectId);

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(3, result.Stages.Count);
        Assert.Equal(10000, result.TotalInputTokens);
        Assert.Equal(26000, result.TotalOutputTokens);
        Assert.Equal(3500, result.TotalCacheReadInputTokens);
        Assert.Equal(850, result.TotalCacheWriteInputTokens);
        Assert.Equal(16, result.TotalTurnCount);
    }

    [Fact]
    public async Task Handle_WithValidProjectId_CallsRepositoryOnce()
    {
        var projectId = Guid.NewGuid();
        _conversationRepositoryMock
            .Setup(repository => repository.GetTokenUsageByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StageTokenUsageSummary>());

        var query = new GetProjectTokenUsageQuery(projectId);

        await _handler.Handle(query, CancellationToken.None);

        _conversationRepositoryMock.Verify(
            repository => repository.GetTokenUsageByProjectIdAsync(projectId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithKnownTokenCounts_CalculatesCostCorrectly()
    {
        var projectId = Guid.NewGuid();
        // Use round numbers to verify pricing formula:
        // Input: 1M tokens × $3.00 = $3.00
        // Output: 1M tokens × $15.00 = $15.00
        // CacheRead: 1M tokens × $0.30 = $0.30
        // CacheWrite: 1M tokens × $3.75 = $3.75
        // Total = $22.05
        var expectedUsage = new List<StageTokenUsageSummary>
        {
            new(Guid.NewGuid(), StageType.RequirementsDiscovery, 1_000_000, 1_000_000, 1_000_000, 1_000_000, 10)
        };

        _conversationRepositoryMock
            .Setup(repository => repository.GetTokenUsageByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUsage);

        var query = new GetProjectTokenUsageQuery(projectId);

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(22.05m, result.TotalEstimatedCost);
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new GetProjectTokenUsageQueryHandler(null!));
    }
}
