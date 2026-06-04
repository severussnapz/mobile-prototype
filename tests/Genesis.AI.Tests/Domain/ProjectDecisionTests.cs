using Genesis.AI.Domain.AggregatesModel.ProjectDecisionAggregate;

namespace Genesis.AI.Tests.Domain;

public class ProjectDecisionTests
{
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    [Fact]
    public void Constructor_WithValidFields_SetsProperties()
    {
        var projectId = Guid.NewGuid();

        var decision = new ProjectDecision(
            projectId, "Use Postgres", "Need a store", "Chose Postgres", "Ops learn it", "ern-1", "Ada", "Lovelace", _timeProvider);

        Assert.Equal(projectId, decision.ProjectId);
        Assert.Equal("Use Postgres", decision.Title);
        Assert.Equal("Need a store", decision.Context);
        Assert.Equal("Chose Postgres", decision.Decision);
        Assert.Equal("Ops learn it", decision.Consequences);
    }

    [Fact]
    public void Constructor_WithBlankTitle_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new ProjectDecision(Guid.NewGuid(), " ", "c", "d", "x", null, null, null, _timeProvider));
    }

    [Fact]
    public void Update_WithNewFields_UpdatesAllFields()
    {
        var decision = new ProjectDecision(
            Guid.NewGuid(), "T", "C", "D", "X", null, null, null, _timeProvider);

        decision.Update("T2", "C2", "D2", "X2", _timeProvider);

        Assert.Equal("T2", decision.Title);
        Assert.Equal("C2", decision.Context);
        Assert.Equal("D2", decision.Decision);
        Assert.Equal("X2", decision.Consequences);
    }
}
