using Genesis.AI.Infrastructure.Services.GitHub;

namespace Genesis.AI.Tests.Infrastructure.Services.GitHub;

public sealed class CodeownersGeneratorTests
{
    private readonly CodeownersGenerator _generator = new();

    [Fact]
    public void Generate_ReturnsExpectedTeamLines()
    {
        var result = _generator.Generate();

        Assert.Contains("@emisgroup/clinical-safety-owners", result);
        Assert.Contains("@emisgroup/ig-owners", result);
        Assert.Contains("@emisgroup/security-owners", result);
        Assert.Contains("Pipeline06", result);
        Assert.Contains("Pipeline07", result);
        Assert.Contains("Pipeline08", result);
    }

    [Fact]
    public void Generate_ContainsNoIndividualNames()
    {
        var result = _generator.Generate();

        Assert.DoesNotContain("@indra", result);
        Assert.DoesNotContain("@idris", result);
    }
}
