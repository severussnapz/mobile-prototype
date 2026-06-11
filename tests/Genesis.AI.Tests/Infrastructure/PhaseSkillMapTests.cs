using System.Reflection;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Infrastructure.Configuration;

namespace Genesis.AI.Tests.Infrastructure;

public class PhaseSkillMapTests
{
    private static readonly HashSet<string> EmbeddedSkillNames = LoadEmbeddedSkillNames();

    // ──────────────────────────────────────────────────────────────────────────
    // AllReferencedSkills — every name in the map resolves to a real file
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AllReferencedSkills_EveryNameResolvesToEmbeddedResource()
    {
        // Arrange
        var referencedSkills = PhaseSkillMap.AllReferencedSkills();

        // Act + Assert
        foreach (var skillName in referencedSkills)
        {
            Assert.True(
                EmbeddedSkillNames.Contains(skillName),
                $"Skill '{skillName}' is referenced in PhaseSkillMap but has no corresponding " +
                $"embedded resource in Genesis.AI.Infrastructure.Skills/. " +
                $"Either add the .md file or remove the reference from the map.");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GetSkillsForPhase — excluded stages return empty
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(StageType.RequirementsDiscovery)]
    [InlineData(StageType.Prototype)]
    public void GetSkillsForPhase_ExcludedStage_ReturnsEmpty(StageType stageType)
    {
        // Act
        var result = PhaseSkillMap.GetSkillsForPhase(stageType, phase: 0);

        // Assert
        Assert.Empty(result);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GetSkillsForPhase — supported stages return universal skills
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(StageType.Architecture)]
    [InlineData(StageType.Design)]
    [InlineData(StageType.Pxd)]
    [InlineData(StageType.ClinicalSafety)]
    [InlineData(StageType.InformationGovernance)]
    [InlineData(StageType.Security)]
    [InlineData(StageType.Normalisation)]
    [InlineData(StageType.Planning)]
    public void GetSkillsForPhase_SupportedStage_ContainsAllUniversalSkills(StageType stageType)
    {
        // Act
        var result = PhaseSkillMap.GetSkillsForPhase(stageType, phase: 1);

        // Assert
        foreach (var expectedSkill in PhaseSkillMap.UniversalSkills)
        {
            Assert.Contains(expectedSkill, result);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GetSkillsForPhase — P06/P07/P08 include human-in-the-loop skills every phase
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(StageType.ClinicalSafety)]
    [InlineData(StageType.InformationGovernance)]
    [InlineData(StageType.Security)]
    public void GetSkillsForPhase_ClinicalOrIgOrSecurityStage_ContainsHumanInTheLoopSkills(StageType stageType)
    {
        // Act
        var result = PhaseSkillMap.GetSkillsForPhase(stageType, phase: 3);

        // Assert
        Assert.Contains("human-in-the-loop-protocol", result);
        Assert.Contains("pre-fill-confidence-markers", result);
    }

    [Theory]
    [InlineData(StageType.Architecture)]
    [InlineData(StageType.Design)]
    [InlineData(StageType.Pxd)]
    public void GetSkillsForPhase_NonClinicalStage_DoesNotContainHumanInTheLoopSkills(StageType stageType)
    {
        // Act
        var result = PhaseSkillMap.GetSkillsForPhase(stageType, phase: 3);

        // Assert
        Assert.DoesNotContain("human-in-the-loop-protocol", result);
        Assert.DoesNotContain("pre-fill-confidence-markers", result);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GetSkillsForPhase — P03 phase 0 includes routing skill
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void GetSkillsForPhase_ArchitecturePhase0_ContainsRunModeRoutingSkill()
    {
        // Act
        var result = PhaseSkillMap.GetSkillsForPhase(StageType.Architecture, phase: 0);

        // Assert
        Assert.Contains("run-mode-routing-p03", result);
    }

    [Fact]
    public void GetSkillsForPhase_ArchitecturePhase1_DoesNotContainRunModeRoutingSkill()
    {
        // Act
        var result = PhaseSkillMap.GetSkillsForPhase(StageType.Architecture, phase: 1);

        // Assert
        Assert.DoesNotContain("run-mode-routing-p03", result);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GetSkillsForPhase — result contains no duplicates
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(StageType.Architecture, 0)]
    [InlineData(StageType.ClinicalSafety, 0)]
    [InlineData(StageType.Security, 5)]
    public void GetSkillsForPhase_Result_ContainsNoDuplicates(StageType stageType, int phase)
    {
        // Act
        var result = PhaseSkillMap.GetSkillsForPhase(stageType, phase);

        // Assert
        var distinct = result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        Assert.Equal(distinct.Count, result.Count);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static HashSet<string> LoadEmbeddedSkillNames()
    {
        var assembly = Assembly.Load("Genesis.AI.Infrastructure");
        const string prefix = "Genesis.AI.Infrastructure.Skills.";
        const string suffix = ".md";

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.StartsWith(prefix, StringComparison.Ordinal) ||
                !resourceName.EndsWith(suffix, StringComparison.Ordinal))
            {
                continue;
            }

            names.Add(resourceName[prefix.Length..^suffix.Length]);
        }

        return names;
    }
}
