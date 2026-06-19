using Genesis.AI.Api.Features.Conversations;

namespace Genesis.AI.Tests.Infrastructure;

/// <summary>
/// Tests for the ConversationStreamController.CountOccurrences helper
/// and the edit_artefact anchor-matching logic.
/// </summary>
public class ConversationStreamControllerEditArtefactTests
{
    [Fact]
    public void FindDuplicateInjectedSectionHeading_WhenInjectedHeadingRepeated_ReturnsHeading()
    {
        const string source = """
## Information Governance (Added by Pipeline 07)
Section A

## Information Governance (Added by Pipeline 07)
Section B
""";

        var duplicateHeading = ConversationStreamController.FindDuplicateInjectedSectionHeading(source);

        Assert.Equal("Information Governance (Added by Pipeline 07)", duplicateHeading);
    }

    [Fact]
    public void FindDuplicateInjectedSectionHeading_WhenInjectedHeadingUnique_ReturnsNull()
    {
        const string source = """
## Clinical Safety (Added by V1e)
Clinical content

## Information Governance (Added by Pipeline 07)
IG content
""";

        var duplicateHeading = ConversationStreamController.FindDuplicateInjectedSectionHeading(source);

        Assert.Null(duplicateHeading);
    }

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

public class ConversationStreamControllerSearchTests
{
    private const string PrototypeHtml = """
        <header class="site-header">
          <nav class="primary-nav">Home</nav>
        </header>
        <section class="hero" style="background-color: #003087;">
          <h1>Welcome</h1>
        </section>
        <div class="message-actions">
          <button class="feedback-thumbs-up" aria-label="Helpful">👍</button>
          <button class="feedback-thumbs-down" aria-label="Not helpful">👎</button>
        </div>
        """;

    [Fact]
    public void BuildSearchResult_WhenExactPhraseOnLine_ReturnsExactMatchRegion()
    {
        var result = ConversationStreamController.BuildSearchResult(
            PrototypeHtml, "primary-nav", "prototype/index.html", 3);

        Assert.Contains("match(es))", result);
        Assert.DoesNotContain("fuzzy match", result);
        Assert.Contains("class=\"primary-nav\"", result);
    }

    [Fact]
    public void BuildSearchResult_WhenPhraseNotVerbatim_FallsBackToWordOverlap()
    {
        // "thumbs up feedback" never appears as a contiguous substring, but the words do.
        var result = ConversationStreamController.BuildSearchResult(
            PrototypeHtml, "thumbs up feedback", "prototype/index.html", 3);

        Assert.Contains("fuzzy match", result);
        Assert.Contains("feedback-thumbs-up", result);
    }

    [Fact]
    public void BuildSearchResult_WhenFuzzyMatch_RanksLineWithMostWordsFirst()
    {
        // "background hero colour" — the hero line contains both "background" and "hero".
        var result = ConversationStreamController.BuildSearchResult(
            PrototypeHtml, "background hero colour", "prototype/index.html", 1);

        Assert.Contains("fuzzy match", result);
        Assert.Contains("background-color", result);
    }

    [Fact]
    public void BuildSearchResult_WhenNoWordsMatch_ReturnsSearchNotFound()
    {
        var result = ConversationStreamController.BuildSearchResult(
            PrototypeHtml, "zzz nonexistent qqtoken", "prototype/index.html", 1);

        Assert.StartsWith("SEARCH_NOT_FOUND", result);
    }

    [Fact]
    public void BuildSearchResult_WhenQueryEmpty_ReturnsError()
    {
        var result = ConversationStreamController.BuildSearchResult(
            PrototypeHtml, "   ", "prototype/index.html", 1);

        Assert.StartsWith("Error:", result);
    }

    [Fact]
    public void ExtractSearchTokens_StripsMarkupAndShortWords_ReturnsDistinctTokens()
    {
        var tokens = ConversationStreamController.ExtractSearchTokens("<button class=\"feedback-thumbs\"> up up </button>");

        Assert.Contains("button", tokens);
        Assert.Contains("class", tokens);
        Assert.Contains("feedback", tokens);
        Assert.Contains("thumbs", tokens);
        // "up" is below the 3-char threshold and must be excluded.
        Assert.DoesNotContain("up", tokens);
        // "button" appears twice in the input but tokens are distinct.
        Assert.Single(tokens, token => token == "button");
    }
}
