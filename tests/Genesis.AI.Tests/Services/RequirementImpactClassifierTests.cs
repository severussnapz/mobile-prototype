using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Services;
using Moq;

namespace Genesis.AI.Tests.Services;

public class RequirementImpactClassifierTests
{
    [Fact]
    public async Task ClassifyAsync_ModelReturnsCosmetic_ReturnsCosmetic()
    {
        var aiServiceMock = new Mock<IAiService>();
        aiServiceMock
            .Setup(service => service.GenerateResponseAsync(
                It.IsAny<AiSystemPrompt>(),
                It.IsAny<IReadOnlyList<AiMessage>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse("cosmetic", 10, 2));

        var classifier = new RequirementImpactClassifier(aiServiceMock.Object);

        var result = await classifier.ClassifyAsync("Change button colour", "Blue button", "Green button", CancellationToken.None);

        Assert.Equal(RequirementImpact.Cosmetic, result);
    }

    [Fact]
    public async Task ClassifyAsync_AiThrows_ReturnsSubstantiveFailSafe()
    {
        var aiServiceMock = new Mock<IAiService>();
        aiServiceMock
            .Setup(service => service.GenerateResponseAsync(
                It.IsAny<AiSystemPrompt>(),
                It.IsAny<IReadOnlyList<AiMessage>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Bedrock failure"));

        var classifier = new RequirementImpactClassifier(aiServiceMock.Object);

        var result = await classifier.ClassifyAsync(null, "Old flow", "New flow", CancellationToken.None);

        Assert.Equal(RequirementImpact.Substantive, result);
    }
}
