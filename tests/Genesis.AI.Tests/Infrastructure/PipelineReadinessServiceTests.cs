using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Services;
using Moq;
using Xunit;

namespace Genesis.AI.Tests.Infrastructure;

public class PipelineReadinessServiceTests
{
    private static readonly Dictionary<string, string> EmptyReqContents = [];
    private static readonly string[] MissingUserStoryViolation = ["Missing required section: '## User Story'"];

    [Fact]
    public async Task GetReadinessAsync_WhenNoBlockers_ReturnsReady()
    {
        var projectId = Guid.NewGuid();
        var repositoryMock = new Mock<IRequirementChangeRepository>();
        var contractValidationMock = new Mock<IContractValidationService>();

        repositoryMock
            .Setup(r => r.HasOpenDefiniteReviewsAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new PipelineReadinessService(
            repositoryMock.Object, contractValidationMock.Object);

        var result = await service.GetReadinessAsync(projectId, EmptyReqContents, CancellationToken.None);

        Assert.True(result.IsReady);
        Assert.Empty(result.Blockers);
    }

    [Fact]
    public async Task GetReadinessAsync_WhenOpenDefiniteReviews_ReturnsNotReady()
    {
        var projectId = Guid.NewGuid();
        var repositoryMock = new Mock<IRequirementChangeRepository>();
        var contractValidationMock = new Mock<IContractValidationService>();

        repositoryMock
            .Setup(r => r.HasOpenDefiniteReviewsAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var change = RequirementChange.Propose(
            projectId, "REQ-001", ChangeType.Gap, "pipeline_05",
            null, "[ ] AC.", "reason", "idris.issa");
        change.Approve("[ ] AC.", ImpactLevel.Definite, ImpactLevel.None,
            ImpactLevel.None, "idris.issa", TimeProvider.System);

        repositoryMock
            .Setup(r => r.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RequirementChange> { change });

        var service = new PipelineReadinessService(
            repositoryMock.Object, contractValidationMock.Object);

        var result = await service.GetReadinessAsync(projectId, EmptyReqContents, CancellationToken.None);

        Assert.False(result.IsReady);
        Assert.NotEmpty(result.Blockers);
        Assert.Contains(result.Blockers, b => b.Contains("review"));
    }

    [Fact]
    public async Task GetReadinessAsync_WhenContractViolations_ReturnsNotReady()
    {
        var projectId = Guid.NewGuid();
        var repositoryMock = new Mock<IRequirementChangeRepository>();
        var contractValidationMock = new Mock<IContractValidationService>();

        repositoryMock
            .Setup(r => r.HasOpenDefiniteReviewsAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        contractValidationMock
            .Setup(s => s.ValidatePipeline01(It.IsAny<string>()))
            .Returns(new ContractValidationResult(false, MissingUserStoryViolation));

        var reqContents = new Dictionary<string, string>
        {
            ["REQ-001"] = "# REQ-001 content without user story"
        };

        var service = new PipelineReadinessService(
            repositoryMock.Object, contractValidationMock.Object);

        var result = await service.GetReadinessAsync(
            projectId, reqContents, CancellationToken.None);

        Assert.False(result.IsReady);
        Assert.Contains(result.Blockers, b => b.Contains("REQ-001"));
    }
}
