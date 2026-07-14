using Genesis.AI.Domain.Enums;
using Genesis.AI.Infrastructure.Services;

namespace Genesis.AI.Tests.Infrastructure;

public class PrototypeReadGuardTests
{
    [Fact]
    public void ValidateGetArtefact_RequirementsPathWhilePrototypeAlreadyBuilt_Rejects()
    {
        var error = PrototypeReadGuard.ValidateGetArtefact(
            stageType: StageType.Prototype,
            filePath: "requirements/REQ-001.md",
            prototypeAlreadyBuilt: true,
            prototypeSingleFile: false);

        Assert.NotNull(error);
        Assert.Contains("prototype/fragments/", error);
    }

    [Fact]
    public void ValidateGetArtefact_RequirementsPathWhilePrototypeNotYetBuilt_ReturnsNull()
    {
        var error = PrototypeReadGuard.ValidateGetArtefact(
            stageType: StageType.Prototype,
            filePath: "requirements/REQ-001.md",
            prototypeAlreadyBuilt: false,
            prototypeSingleFile: false);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateGetArtefact_FragmentPathWhilePrototypeAlreadyBuilt_ReturnsNull()
    {
        var error = PrototypeReadGuard.ValidateGetArtefact(
            stageType: StageType.Prototype,
            filePath: "prototype/fragments/screen-01.html",
            prototypeAlreadyBuilt: true,
            prototypeSingleFile: false);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateGetArtefact_RequirementsPathOnNonPrototypeStage_ReturnsNull()
    {
        var error = PrototypeReadGuard.ValidateGetArtefact(
            stageType: StageType.Architecture,
            filePath: "requirements/REQ-001.md",
            prototypeAlreadyBuilt: true,
            prototypeSingleFile: false);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateGetArtefact_RequirementsPathInSingleFileMode_ReturnsNull()
    {
        var error = PrototypeReadGuard.ValidateGetArtefact(
            stageType: StageType.Prototype,
            filePath: "requirements/REQ-001.md",
            prototypeAlreadyBuilt: true,
            prototypeSingleFile: true);

        Assert.Null(error);
    }
}
