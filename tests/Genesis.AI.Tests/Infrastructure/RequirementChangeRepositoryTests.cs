using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;
using Genesis.AI.Infrastructure.Services;
using Xunit;

namespace Genesis.AI.Tests.Infrastructure;

public class RequirementChangeRepositoryTests
{
    [Fact]
    public void RequirementChangeRepository_ImplementsIRequirementChangeRepository()
    {
        var repositoryType = typeof(Genesis.AI.Infrastructure.Repositories.RequirementChangeRepository);
        var interfaceType = typeof(IRequirementChangeRepository);
        Assert.True(interfaceType.IsAssignableFrom(repositoryType));
    }

    [Fact]
    public void HasOpenDefiniteReviews_WhenAllNone_QueryFiltersCorrectly()
    {
        // The actual DB query is tested in integration tests.
        // This test verifies the domain logic that feeds the query.
        var change = RequirementChange.Propose(
            projectId: Guid.NewGuid(),
            reqId: "REQ-001",
            changeType: ChangeType.Gap,
            raisingPipeline: "pipeline_05",
            raisingPipelineConversationId: null,
            proposedAcText: "[ ] AC text.",
            rationale: "reason",
            createdBy: "idris.issa");

        change.Approve("[ ] AC text.", ImpactLevel.None, ImpactLevel.None,
            ImpactLevel.None, "idris.issa", TimeProvider.System);

        Assert.False(change.HasOpenDefiniteReviews());
    }

    [Fact]
    public void HasOpenDefiniteReviews_WhenDefiniteAndUnreviewed_ReturnsTrue()
    {
        var change = RequirementChange.Propose(
            projectId: Guid.NewGuid(),
            reqId: "REQ-001",
            changeType: ChangeType.Gap,
            raisingPipeline: "pipeline_05",
            raisingPipelineConversationId: null,
            proposedAcText: "[ ] AC text.",
            rationale: "reason",
            createdBy: "idris.issa");

        change.Approve("[ ] AC text.", ImpactLevel.Definite, ImpactLevel.None,
            ImpactLevel.None, "idris.issa", TimeProvider.System);

        Assert.True(change.HasOpenDefiniteReviews());
    }
}
