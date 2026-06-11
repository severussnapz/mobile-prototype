using Genesis.AI.Domain.Enums;
using Genesis.AI.Infrastructure.Services;

namespace Genesis.AI.Tests.Services;

public class StageFoundationMapTests
{
    // ── Stages with no foundation prefixes ─────────────────────────────────

    [Theory]
    [InlineData(StageType.RequirementsDiscovery)]
    [InlineData(StageType.Prototype)]
    public void GetFoundationPrefixes_ForP1P2Stages_ReturnsEmpty(StageType stageType)
    {
        var prefixes = StageFoundationMap.GetFoundationPrefixes(stageType);

        Assert.Empty(prefixes);
    }

    // ── P3 Architecture: only requirements/ ────────────────────────────────

    [Fact]
    public void GetFoundationPrefixes_ForArchitecture_ContainsRequirementsPrefix()
    {
        var prefixes = StageFoundationMap.GetFoundationPrefixes(StageType.Architecture);

        Assert.Contains("requirements/", prefixes);
        Assert.DoesNotContain("architecture/", prefixes);
    }

    // ── P4 Design: requirements/ + architecture/ ───────────────────────────

    [Fact]
    public void GetFoundationPrefixes_ForDesign_ContainsRequirementsAndArchitecturePrefixes()
    {
        var prefixes = StageFoundationMap.GetFoundationPrefixes(StageType.Design);

        Assert.Contains("requirements/", prefixes);
        Assert.Contains("architecture/", prefixes);
        Assert.DoesNotContain("design/", prefixes);
    }

    // ── P5 Pxd: requirements/ + architecture/ + design/ ───────────────────

    [Fact]
    public void GetFoundationPrefixes_ForPxd_ContainsRequirementsArchitectureAndDesignPrefixes()
    {
        var prefixes = StageFoundationMap.GetFoundationPrefixes(StageType.Pxd);

        Assert.Contains("requirements/", prefixes);
        Assert.Contains("architecture/", prefixes);
        Assert.Contains("design/", prefixes);
    }

    // ── IsFoundationArtefact: prefix matching ──────────────────────────────

    [Theory]
    [InlineData("requirements/REQ-001.md", true)]
    [InlineData("requirements/REQ-999.md", true)]
    [InlineData("requirements/subfolder/nested.md", true)]
    [InlineData("architecture/ARCH-001.md", false)]
    [InlineData("design/wireframes.md", false)]
    [InlineData("manifest.md", false)]
    public void IsFoundationArtefact_ForArchitectureStage_MatchesOnlyRequirementsPrefix(string filePath, bool expectedMatch)
    {
        var result = StageFoundationMap.IsFoundationArtefact(StageType.Architecture, filePath);

        Assert.Equal(expectedMatch, result);
    }

    [Theory]
    [InlineData("requirements/REQ-001.md", true)]
    [InlineData("architecture/ARCH-001.md", true)]
    [InlineData("design/wireframes.md", false)]
    [InlineData("manifest.md", false)]
    public void IsFoundationArtefact_ForDesignStage_MatchesRequirementsAndArchitecturePrefixes(string filePath, bool expectedMatch)
    {
        var result = StageFoundationMap.IsFoundationArtefact(StageType.Design, filePath);

        Assert.Equal(expectedMatch, result);
    }

    [Fact]
    public void IsFoundationArtefact_ForP1Stage_AlwaysReturnsFalse()
    {
        var result = StageFoundationMap.IsFoundationArtefact(
            StageType.RequirementsDiscovery, "requirements/REQ-001.md");

        Assert.False(result);
    }

    [Fact]
    public void IsFoundationArtefact_PathMatchIsCaseInsensitive()
    {
        var result = StageFoundationMap.IsFoundationArtefact(
            StageType.Architecture, "REQUIREMENTS/REQ-001.MD");

        Assert.True(result);
    }

    [Fact]
    public void IsFoundationArtefact_ManifestMdIsNeverAFoundationArtefact()
    {
        // manifest.md is Category C (live tracking) — must never be included in foundation
        foreach (var stage in Enum.GetValues<StageType>())
        {
            var result = StageFoundationMap.IsFoundationArtefact(stage, "manifest.md");
            Assert.False(result, $"manifest.md should not be a foundation artefact for {stage}");
        }
    }
}
