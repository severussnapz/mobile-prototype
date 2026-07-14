using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Genesis.AI.Tests.Infrastructure;

public sealed class ActiveSkillsServiceTests
{
    private readonly Mock<ISkillContentService> _skillContentServiceMock = new();

    private ActiveSkillsService CreateSut() =>
        new(_skillContentServiceMock.Object, NullLogger<ActiveSkillsService>.Instance);

    // ── RequirementsDiscovery wiring ─────────────────────────────────────────

    [Fact]
    public async Task BuildActiveSkillsAsync_RequirementsDiscoveryStage_ReturnsMappedContent()
    {
        // Arrange
        _skillContentServiceMock
            .Setup(service => service.GetSkillContent(It.IsAny<string>()))
            .Returns<string>(name => $"content-of-{name}");

        var sut = CreateSut();

        // Act
        var result = await sut.BuildActiveSkillsAsync(StageType.RequirementsDiscovery, 0, CancellationToken.None);

        // Assert
        Assert.Contains("content-of-requirements-elicitation", result);
        _skillContentServiceMock.Verify(service => service.GetSkillContent("requirements-elicitation"), Times.Once);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task BuildActiveSkillsAsync_SupportedStage_ReturnsContentForEachSkill()
    {
        // Arrange
        _skillContentServiceMock
            .Setup(service => service.GetSkillContent(It.IsAny<string>()))
            .Returns<string>(name => $"content-of-{name}");

        var sut = CreateSut();

        // Act
        var result = await sut.BuildActiveSkillsAsync(StageType.Architecture, 1, CancellationToken.None);

        // Assert
        Assert.NotEmpty(result);

        // Universal skills must all be present
        Assert.Contains("content-of-interview-discipline", result);
        Assert.Contains("content-of-parking-lot", result);
        Assert.Contains("content-of-phase-transition-protocol", result);
        Assert.Contains("content-of-bounded-clarification-budget", result);
        Assert.Contains("content-of-carry-forward-contract", result);
        Assert.Contains("content-of-tool-failure-policy", result);
    }

    [Fact]
    public async Task BuildActiveSkillsAsync_MultipleSkillsResolved_JoinsSkillsWithSeparator()
    {
        // Arrange
        _skillContentServiceMock
            .Setup(service => service.GetSkillContent(It.IsAny<string>()))
            .Returns<string>(name => $"content-of-{name}");

        var sut = CreateSut();

        // Act
        var result = await sut.BuildActiveSkillsAsync(StageType.Architecture, 1, CancellationToken.None);

        // Assert — separator between each skill block
        Assert.Contains("\n\n---\n\n", result);
    }

    [Fact]
    public async Task BuildActiveSkillsAsync_PhaseZeroArchitecture_IncludesRunModeRoutingSkill()
    {
        // Arrange
        _skillContentServiceMock
            .Setup(service => service.GetSkillContent(It.IsAny<string>()))
            .Returns<string>(name => $"content-of-{name}");

        var sut = CreateSut();

        // Act
        var result = await sut.BuildActiveSkillsAsync(StageType.Architecture, 0, CancellationToken.None);

        // Assert
        Assert.Contains("content-of-run-mode-routing-p03", result);
    }

    [Fact]
    public async Task BuildActiveSkillsAsync_PhaseOneArchitecture_DoesNotIncludeRunModeRoutingSkill()
    {
        // Arrange
        _skillContentServiceMock
            .Setup(service => service.GetSkillContent(It.IsAny<string>()))
            .Returns<string>(name => $"content-of-{name}");

        var sut = CreateSut();

        // Act
        var result = await sut.BuildActiveSkillsAsync(StageType.Architecture, 1, CancellationToken.None);

        // Assert
        Assert.DoesNotContain("content-of-run-mode-routing-p03", result);
    }

    [Theory]
    [InlineData(StageType.ClinicalSafety)]
    [InlineData(StageType.InformationGovernance)]
    [InlineData(StageType.Security)]
    public async Task BuildActiveSkillsAsync_HighAssuranceStage_IncludesHumanInTheLoopContent(StageType stageType)
    {
        // Arrange
        _skillContentServiceMock
            .Setup(service => service.GetSkillContent(It.IsAny<string>()))
            .Returns<string>(name => $"content-of-{name}");

        var sut = CreateSut();

        // Act
        var result = await sut.BuildActiveSkillsAsync(stageType, 0, CancellationToken.None);

        // Assert
        Assert.Contains("content-of-human-in-the-loop-protocol", result);
        Assert.Contains("content-of-pre-fill-confidence-markers", result);
    }

    // ── Missing skill resilience ───────────────────────────────────────────────

    [Fact]
    public async Task BuildActiveSkillsAsync_SkillNotFound_OmitsItAndContinues()
    {
        // Arrange — all skills return content except "parking-lot"
        _skillContentServiceMock
            .Setup(service => service.GetSkillContent("parking-lot"))
            .Returns((string?)null);

        _skillContentServiceMock
            .Setup(service => service.GetSkillContent(It.Is<string>(name => name != "parking-lot")))
            .Returns<string>(name => $"content-of-{name}");

        var sut = CreateSut();

        // Act
        var result = await sut.BuildActiveSkillsAsync(StageType.Architecture, 1, CancellationToken.None);

        // Assert — other skills still present; missing skill absent
        Assert.Contains("content-of-interview-discipline", result);
        Assert.DoesNotContain("content-of-parking-lot", result);
    }

    [Fact]
    public async Task BuildActiveSkillsAsync_AllSkillsNotFound_ReturnsEmptyString()
    {
        // Arrange
        _skillContentServiceMock
            .Setup(service => service.GetSkillContent(It.IsAny<string>()))
            .Returns((string?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.BuildActiveSkillsAsync(StageType.Architecture, 1, CancellationToken.None);

        // Assert
        Assert.Equal(string.Empty, result);
    }
}
