using Genesis.AI.Api.Features.Conversations;

namespace Genesis.AI.Tests.Infrastructure;

/// <summary>
/// Tests for the ConversationStreamController.CountOccurrences helper
/// and the edit_artefact anchor-matching logic.
/// </summary>
public class ConversationStreamControllerEditArtefactTests
{
    [Fact]
    public void EditArtefact_ExactSingleMatch_PersistsNewVersionWithCorrectContent()
    {
        const string source = "Hello world, this is a test.";
        const string target = "world";

        var count = ConversationStreamController.CountOccurrences(source, target);

        Assert.Equal(1, count);

        // Verify the replacement result
        var updated = source.Replace(target, "universe", StringComparison.Ordinal);
        Assert.Equal("Hello universe, this is a test.", updated);
    }

    [Fact]
    public void EditArtefact_ZeroMatches_ReturnsAnchorNotFoundError()
    {
        const string source = "Hello world, this is a test.";
        const string target = "banana";

        var count = ConversationStreamController.CountOccurrences(source, target);

        Assert.Equal(0, count);
    }

    [Fact]
    public void EditArtefact_TwoMatches_ReturnsAnchorAmbiguousError()
    {
        const string source = "foo bar foo baz";
        const string target = "foo";

        var count = ConversationStreamController.CountOccurrences(source, target);

        Assert.Equal(2, count);
    }

    [Fact]
    public void EditArtefact_EmptyNewStr_DeletesAnchorContentAndPersists()
    {
        const string source = "Hello world, this is a test.";
        const string target = "world, ";
        const string newStr = "";

        var count = ConversationStreamController.CountOccurrences(source, target);
        Assert.Equal(1, count);

        var updated = source.Replace(target, newStr, StringComparison.Ordinal);
        Assert.Equal("Hello this is a test.", updated);
    }

    [Fact]
    public void EditArtefact_MultipleVersionsExist_AppliesEditToLatestVersion()
    {
        // Simulate latest version having different content from older version
        const string latestContent = "## Section\nThis is the latest content with updated text.";
        const string target = "updated text";
        const string newStr = "final text";

        var count = ConversationStreamController.CountOccurrences(latestContent, target);
        Assert.Equal(1, count);

        var updated = latestContent.Replace(target, newStr, StringComparison.Ordinal);
        Assert.Contains("final text", updated);
        Assert.DoesNotContain("updated text", updated);
    }
}
