using Genesis.AI.Domain.Commands.GenerateSessionClose;
using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Tests.Domain.Commands.GenerateSessionClose;

public sealed class SessionCloseStageMapTests
{
    [Fact]
    public void StageType_RequirementsDiscovery_MapsToP01()
    {
        var result = SessionCloseStageMap.GetFilePath(StageType.RequirementsDiscovery);

        Assert.Equal("session-close/SESSION-CLOSE-P01.md", result);
    }

    [Fact]
    public void StageType_Prototype_MapsToP02()
    {
        var result = SessionCloseStageMap.GetFilePath(StageType.Prototype);

        Assert.Equal("session-close/SESSION-CLOSE-P02.md", result);
    }

    [Fact]
    public void StageType_Architecture_MapsToP03()
    {
        var result = SessionCloseStageMap.GetFilePath(StageType.Architecture);

        Assert.Equal("session-close/SESSION-CLOSE-P03.md", result);
    }

    [Fact]
    public void StageType_Design_MapsToP04()
    {
        var result = SessionCloseStageMap.GetFilePath(StageType.Design);

        Assert.Equal("session-close/SESSION-CLOSE-P04.md", result);
    }

    [Fact]
    public void StageType_Pxd_MapsToP05()
    {
        var result = SessionCloseStageMap.GetFilePath(StageType.Pxd);

        Assert.Equal("session-close/SESSION-CLOSE-P05.md", result);
    }

    [Fact]
    public void StageType_ClinicalSafety_MapsToP06()
    {
        var result = SessionCloseStageMap.GetFilePath(StageType.ClinicalSafety);

        Assert.Equal("session-close/SESSION-CLOSE-P06.md", result);
    }

    [Fact]
    public void StageType_InformationGovernance_MapsToP07()
    {
        var result = SessionCloseStageMap.GetFilePath(StageType.InformationGovernance);

        Assert.Equal("session-close/SESSION-CLOSE-P07.md", result);
    }

    [Fact]
    public void StageType_Security_MapsToP08()
    {
        var result = SessionCloseStageMap.GetFilePath(StageType.Security);

        Assert.Equal("session-close/SESSION-CLOSE-P08.md", result);
    }

    [Fact]
    public void StageType_Normalisation_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() => SessionCloseStageMap.GetFilePath(StageType.Normalisation));
    }

    [Fact]
    public void StageType_Planning_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() => SessionCloseStageMap.GetFilePath(StageType.Planning));
    }
}