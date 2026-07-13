using Genesis.AI.Api.Features.Conversations;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Tests.Infrastructure;

public class ConversationStreamControllerPrototypeSingleFileGuardTests
{
    [Fact]
    public void ShouldBlockPrototypeRegenerationRead_SingleFileModeEnabled_ReturnsFalse()
    {
        var blocked = ConversationStreamController.ShouldBlockPrototypeRegenerationRead(
            StageType.Prototype,
            "prototype/index.html",
            prototypeSingleFile: true);

        Assert.False(blocked);
    }

    [Fact]
    public void BuildPrototypeIntentRoutingDirective_SingleFileModeEnabled_ReturnsEmpty()
    {
        var artefactManifest = new List<Artefact>
        {
            Artefact.CreateS3Artefact(
                Guid.NewGuid(),
                version: 1,
                filePath: "prototype/index.html",
                s3Key: "projects/p/artefacts/prototype/index.html/v1",
                contentType: "text/html",
                sizeBytes: 1,
                createdBy: "tester",
                timeProvider: TimeProvider.System,
                isPublished: true)
        };

        var directive = ConversationStreamController.BuildPrototypeIntentRoutingDirective(
            StageType.Prototype,
            "Update the header colour",
            artefactManifest,
            prototypeSingleFile: true);

        Assert.Equal(string.Empty, directive);
    }

    [Fact]
    public void ShouldBlockPrototypeRegenerationSave_SingleFileModeEnabled_ReturnsFalse()
    {
        var blocked = ConversationStreamController.ShouldBlockPrototypeRegenerationSave(
            StageType.Prototype,
            "prototype/index.html",
            prototypeSingleFile: true,
            prototypeAlreadyExists: true,
            contentIsLargeForEditing: false);

        Assert.False(blocked);
    }
}
