using Genesis.AI.Domain.Enums;
using Genesis.AI.Infrastructure.Services;

namespace Genesis.AI.Tests.Infrastructure.Services;

public sealed class SessionCloseSkillBuilderTests
{
    [Fact]
    public void Build_ContainsStageNameInOutput()
    {
        var builder = new SessionCloseSkillBuilder();

        var output = builder.Build(StageType.ClinicalSafety, "summary text");

        var containsClinicalSafety = output.Contains("Clinical Safety", StringComparison.Ordinal);
        var containsP06 = output.Contains("P06", StringComparison.Ordinal);

        Assert.True(containsClinicalSafety || containsP06);
    }

    [Fact]
    public void Build_ContainsSummaryInOutput()
    {
        var builder = new SessionCloseSkillBuilder();

        var output = builder.Build(StageType.RequirementsDiscovery, "this is the summary");

        Assert.Contains("this is the summary", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_DifferentStages_ProduceDifferentOutput()
    {
        var builder = new SessionCloseSkillBuilder();

        var p01 = builder.Build(StageType.RequirementsDiscovery, "same summary");
        var p06 = builder.Build(StageType.ClinicalSafety, "same summary");

        Assert.NotEqual(p01, p06);
    }
}