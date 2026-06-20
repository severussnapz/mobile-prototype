using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Services;
using Moq;
using Xunit;

namespace Genesis.AI.Tests.Infrastructure;

public class ApplyToScopeStrategyTests
{
    [Fact]
    public async Task DeriveValuesAsync_WhenLiteralStrategy_AppliesSameValueToAllElements()
    {
        var matches = new List<PrototypeDomSearchMatch>
        {
            BuildMatch("prototype/fragments/screen-01.html|A1", "Button One"),
            BuildMatch("prototype/fragments/screen-01.html|A2", "Button Two"),
            BuildMatch("prototype/fragments/screen-01.html|A3", "Button Three"),
        };

        var strategy = new LiteralStrategy();
        var results = await strategy.DeriveValuesAsync(matches, "btn-primary", CancellationToken.None);

        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.Equal("btn-primary", r.Value));
    }

    [Fact]
    public async Task DeriveValuesAsync_WhenLiteralStrategy_PreservesNodeKeyFromMatch()
    {
        var matches = new List<PrototypeDomSearchMatch>
        {
            BuildMatch("prototype/fragments/screen-01.html|A1", "Save"),
        };

        var strategy = new LiteralStrategy();
        var results = await strategy.DeriveValuesAsync(matches, "active", CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("prototype/fragments/screen-01.html|A1", results[0].NodeKey);
    }

    [Fact]
    public async Task DeriveValuesAsync_WhenDeriveFromTextContentStrategy_StripsLeadingEmoji()
    {
        var matches = new List<PrototypeDomSearchMatch>
        {
            BuildMatch("prototype/fragments/screen-01.html|A1", "🖨️ Print"),
            BuildMatch("prototype/fragments/screen-01.html|A2", "◀ Previous"),
            BuildMatch("prototype/fragments/screen-01.html|A3", "Save & close"),
        };

        var strategy = new DeriveFromTextContentStrategy();
        var results = await strategy.DeriveValuesAsync(matches, null, CancellationToken.None);

        Assert.Equal("Print", results[0].Value);
        Assert.Equal("Previous", results[1].Value);
        Assert.Equal("Save & close", results[2].Value);
    }

    [Fact]
    public async Task DeriveValuesAsync_WhenDeriveFromTextContentStrategy_DeduplicatesRepeatedWords()
    {
        var matches = new List<PrototypeDomSearchMatch>
        {
            BuildMatch("prototype/fragments/screen-01.html|A1", "Save Save"),
            BuildMatch("prototype/fragments/screen-01.html|A2", "Close"),
        };

        var strategy = new DeriveFromTextContentStrategy();
        var results = await strategy.DeriveValuesAsync(matches, null, CancellationToken.None);

        Assert.Equal("Save", results[0].Value);
        Assert.Equal("Close", results[1].Value);
    }

    [Fact]
    public async Task DeriveValuesAsync_WhenDeriveFromTextContentStrategy_StripsArrowPrefixes()
    {
        var matches = new List<PrototypeDomSearchMatch>
        {
            BuildMatch("prototype/fragments/screen-01.html|A1", "▶ Next page"),
            BuildMatch("prototype/fragments/screen-01.html|A2", "► Forward"),
        };

        var strategy = new DeriveFromTextContentStrategy();
        var results = await strategy.DeriveValuesAsync(matches, null, CancellationToken.None);

        Assert.Equal("Next page", results[0].Value);
        Assert.Equal("Forward", results[1].Value);
    }

    private static PrototypeDomSearchMatch BuildMatch(string nodeKey, string textSnippet)
    {
        return new PrototypeDomSearchMatch(
            NodeKey: nodeKey,
            FragmentPath: nodeKey.Split('|')[0],
            TagName: "button",
            TextSnippet: textSnippet,
            CssSelector: "button",
            ClassList: [],
            ParentContext: "",
            SiblingContext: "");
    }
}

public class GenerateFromContextStrategyTests
{
    [Fact]
    public async Task DeriveValuesAsync_WhenAiReturnsMatchingSnippets_ReturnsCorrectValues()
    {
        var matches = new List<PrototypeDomSearchMatch>
        {
            BuildMatch("prototype/fragments/screen-01.html|A1", "Hide queue"),
            BuildMatch("prototype/fragments/screen-01.html|A2", "Save & close"),
            BuildMatch("prototype/fragments/screen-01.html|A3", "Print"),
        };

        var aiJson = """
            [
              {"text_snippet":"Hide queue","value":"Hide document queue panel"},
              {"text_snippet":"Save & close","value":"Save and close document"},
              {"text_snippet":"Print","value":"Print document"}
            ]
            """;

        var mockAiService = new Mock<IAiService>();
        mockAiService
            .Setup(s => s.GenerateResponseAsync(
                It.IsAny<AiSystemPrompt>(),
                It.IsAny<IReadOnlyList<AiMessage>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse(aiJson, 100, 50));

        var strategy = new GenerateFromContextStrategy(mockAiService.Object);
        var results = await strategy.DeriveValuesAsync(matches, null, CancellationToken.None);

        Assert.Equal(3, results.Count);
        Assert.Equal("Hide document queue panel", results.Single(r => r.NodeKey.Contains("A1")).Value);
        Assert.Equal("Save and close document", results.Single(r => r.NodeKey.Contains("A2")).Value);
        Assert.Equal("Print document", results.Single(r => r.NodeKey.Contains("A3")).Value);
    }

    [Fact]
    public async Task DeriveValuesAsync_WhenSnippetNotMatchedInAiResponse_ReturnsEmptyValue()
    {
        var matches = new List<PrototypeDomSearchMatch>
        {
            BuildMatch("prototype/fragments/screen-01.html|A1", "Unknown button"),
        };

        var aiJson = """
            [{"text_snippet":"Something else entirely","value":"Some value"}]
            """;

        var mockAiService = new Mock<IAiService>();
        mockAiService
            .Setup(s => s.GenerateResponseAsync(
                It.IsAny<AiSystemPrompt>(),
                It.IsAny<IReadOnlyList<AiMessage>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse(aiJson, 100, 50));

        var strategy = new GenerateFromContextStrategy(mockAiService.Object);
        var results = await strategy.DeriveValuesAsync(matches, null, CancellationToken.None);

        Assert.Single(results);
        Assert.Equal(string.Empty, results[0].Value);
    }

    [Fact]
    public async Task DeriveValuesAsync_WhenAiReturnsInvalidJson_ReturnsEmptyValuesForAllMatches()
    {
        var matches = new List<PrototypeDomSearchMatch>
        {
            BuildMatch("prototype/fragments/screen-01.html|A1", "Save"),
        };

        var mockAiService = new Mock<IAiService>();
        mockAiService
            .Setup(s => s.GenerateResponseAsync(
                It.IsAny<AiSystemPrompt>(),
                It.IsAny<IReadOnlyList<AiMessage>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse("not valid json", 100, 50));

        var strategy = new GenerateFromContextStrategy(mockAiService.Object);
        var results = await strategy.DeriveValuesAsync(matches, null, CancellationToken.None);

        Assert.Single(results);
        Assert.Equal(string.Empty, results[0].Value);
    }

    private static PrototypeDomSearchMatch BuildMatch(string nodeKey, string textSnippet)
    {
        return new PrototypeDomSearchMatch(
            NodeKey: nodeKey,
            FragmentPath: nodeKey.Split('|')[0],
            TagName: "button",
            TextSnippet: textSnippet,
            CssSelector: "button",
            ClassList: [],
            ParentContext: "",
            SiblingContext: "");
    }
}
