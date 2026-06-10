using Genesis.AI.Domain.Enums;
using Genesis.AI.Infrastructure.Services;

namespace Genesis.AI.Tests.Services;

public class EmbeddedPromptServiceTests
{
    private readonly EmbeddedPromptService _service = new();

    [Theory]
    [InlineData(StageType.RequirementsDiscovery)]
    [InlineData(StageType.Prototype)]
    [InlineData(StageType.Architecture)]
    [InlineData(StageType.Design)]
    [InlineData(StageType.Pxd)]
    [InlineData(StageType.ClinicalSafety)]
    [InlineData(StageType.Normalisation)]
    [InlineData(StageType.Planning)]
    public void GetSystemPrompt_ForPipelineStage_ReturnsNonEmptyPrompt(StageType stageType)
    {
        var prompt = _service.GetSystemPrompt(stageType);

        Assert.False(string.IsNullOrWhiteSpace(prompt));
    }

    [Fact]
    public void GetTotalPhases_ForRequirementsDiscovery_ReturnsTwelve()
    {
        var totalPhases = _service.GetTotalPhases(StageType.RequirementsDiscovery);

        Assert.Equal(12, totalPhases);
    }

    [Fact]
    public void GetPhaseNames_ForRequirementsDiscovery_ReturnsOrderedPhaseList()
    {
        var phaseNames = _service.GetPhaseNames(StageType.RequirementsDiscovery);

        Assert.Equal("mode_selection", phaseNames[0]);
        Assert.Contains("feedback", phaseNames);
    }

    [Fact]
    public void GetPhaseNames_ForArchitecture_ReturnsArchitecturePhases()
    {
        var phaseNames = _service.GetPhaseNames(StageType.Architecture);

        Assert.Contains("bdat_analysis", phaseNames);
        Assert.Contains("adr_creation", phaseNames);
    }
}
