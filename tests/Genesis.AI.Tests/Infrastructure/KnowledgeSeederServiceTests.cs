using Genesis.AI.Infrastructure.Services;
using Xunit;

namespace Genesis.AI.Tests.Infrastructure;

/// <summary>
/// Tests for KnowledgeSeederService resource filtering and path building logic.
/// These tests verify that the seeder correctly identifies resources to include/exclude
/// and builds the correct source paths for indexing.
/// </summary>
public class KnowledgeSeederServiceTests
{
    [Fact]
    public void ShouldExcludeResource_WhenContainsAiNewTmp_ReturnsTrue()
    {
        // Arrange
        var resourceName = "Genesis.AI.Infrastructure.Prompts._ai_new_tmp.SomeFile.md";

        // Act
        var result = KnowledgeSeederService.ShouldExcludeResource(resourceName);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldExcludeResource_WhenNotMarkdown_ReturnsTrue()
    {
        // Arrange
        var resourceName = "Genesis.AI.Infrastructure.Prompts.Pipeline01RequirementsDiscovery.txt";

        // Act
        var result = KnowledgeSeederService.ShouldExcludeResource(resourceName);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldExcludeResource_WhenValidPromptResource_ReturnsFalse()
    {
        // Arrange
        var resourceName = "Genesis.AI.Infrastructure.Prompts.Pipeline01RequirementsDiscovery.md";

        // Act
        var result = KnowledgeSeederService.ShouldExcludeResource(resourceName);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void BuildSourcePath_WhenPromptsResource_ReturnsCorrectPath()
    {
        // Arrange
        var resourceName = "Genesis.AI.Infrastructure.Prompts.Pipeline01RequirementsDiscovery.md";

        // Act
        var result = KnowledgeSeederService.BuildSourcePath(resourceName);

        // Assert
        Assert.Equal("Prompts/Pipeline01RequirementsDiscovery.md", result);
    }

    [Fact]
    public void BuildSourcePath_WhenKnowledgeBaseResource_ReturnsCorrectPath()
    {
        // Arrange
        var resourceName = "Genesis.AI.Infrastructure.KnowledgeBase.genesis-ai-master-plan.md";

        // Act
        var result = KnowledgeSeederService.BuildSourcePath(resourceName);

        // Assert
        Assert.Equal("KnowledgeBase/genesis-ai-master-plan.md", result);
    }
}
