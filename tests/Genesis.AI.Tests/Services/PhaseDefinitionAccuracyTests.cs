using Genesis.AI.Domain.Enums;
using Genesis.AI.Infrastructure.Services;

namespace Genesis.AI.Tests.Services;

public sealed class PhaseDefinitionAccuracyTests
{
    private readonly EmbeddedPromptService _service = new();

    [Fact]
    public void GetPhaseNames_P01_RequirementsDiscovery_HasCorrectPhases()
    {
        var phaseNames = _service.GetPhaseNames(StageType.RequirementsDiscovery);

        Assert.Equal(7, phaseNames.Length);
        Assert.Equal("business_context", phaseNames[0]);
        Assert.Equal("finalisation", phaseNames[^1]);
        Assert.Equal(7, _service.GetTotalPhases(StageType.RequirementsDiscovery));
    }

    [Fact]
    public void GetPhaseNames_P03_Architecture_HasCorrectPhases()
    {
        var phaseNames = _service.GetPhaseNames(StageType.Architecture);

        Assert.Equal(14, phaseNames.Length);
        Assert.Equal("context_loading", phaseNames[0]);
        Assert.Equal("feedback", phaseNames[^1]);
        Assert.Equal(14, _service.GetTotalPhases(StageType.Architecture));
    }

    [Fact]
    public void GetPhaseNames_P04_Design_HasCorrectPhases()
    {
        var phaseNames = _service.GetPhaseNames(StageType.Design);

        Assert.Equal(14, phaseNames.Length);
        Assert.Equal("context_loading", phaseNames[0]);
        Assert.Equal("feedback", phaseNames[^1]);
        Assert.Equal(14, _service.GetTotalPhases(StageType.Design));
    }

    [Fact]
    public void GetPhaseNames_P05_Pxd_HasCorrectPhases()
    {
        var phaseNames = _service.GetPhaseNames(StageType.Pxd);

        Assert.Equal(14, phaseNames.Length);
        Assert.Equal("context_loading", phaseNames[0]);
        Assert.Equal("feedback", phaseNames[^1]);
        Assert.Equal(14, _service.GetTotalPhases(StageType.Pxd));
    }

    [Fact]
    public void GetPhaseNames_P06_ClinicalSafety_HasCorrectPhases()
    {
        var phaseNames = _service.GetPhaseNames(StageType.ClinicalSafety);

        Assert.Equal(13, phaseNames.Length);
        Assert.Equal("context_loading", phaseNames[0]);
        Assert.Equal("cso_final_review", phaseNames[^1]);
        Assert.Equal(13, _service.GetTotalPhases(StageType.ClinicalSafety));
    }

    [Fact]
    public void GetPhaseNames_P07_InformationGovernance_HasCorrectPhases()
    {
        var phaseNames = _service.GetPhaseNames(StageType.InformationGovernance);

        Assert.Equal(8, phaseNames.Length);
        Assert.Equal("context_loading", phaseNames[0]);
        Assert.Equal("feedback", phaseNames[^1]);
        Assert.Equal(8, _service.GetTotalPhases(StageType.InformationGovernance));
    }

    [Fact]
    public void GetPhaseNames_P08_Security_HasCorrectPhases()
    {
        var phaseNames = _service.GetPhaseNames(StageType.Security);

        Assert.Equal(8, phaseNames.Length);
        Assert.Equal("context_loading", phaseNames[0]);
        Assert.Equal("feedback", phaseNames[^1]);
        Assert.Equal(8, _service.GetTotalPhases(StageType.Security));
    }

    [Fact]
    public void GetPhaseNames_P09_Normalisation_HasCorrectPhases()
    {
        var phaseNames = _service.GetPhaseNames(StageType.Normalisation);

        Assert.Equal(4, phaseNames.Length);
        Assert.Equal("intake_and_plan", phaseNames[0]);
        Assert.Equal("handoff", phaseNames[^1]);
        Assert.Equal(4, _service.GetTotalPhases(StageType.Normalisation));
    }

    [Fact]
    public void GetPhaseNames_P10_Planning_HasCorrectPhases()
    {
        var phaseNames = _service.GetPhaseNames(StageType.Planning);

        Assert.Equal(5, phaseNames.Length);
        Assert.Equal("intake", phaseNames[0]);
        Assert.Equal("confirmed_ready", phaseNames[^1]);
        Assert.Equal(5, _service.GetTotalPhases(StageType.Planning));
    }
}
