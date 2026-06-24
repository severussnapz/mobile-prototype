using Genesis.AI.Api.Features.Conversations;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Tests.Infrastructure;

/// <summary>
/// Tests for ConversationStreamController.BuildDomSearchMultiMatchResult — the multi-match
/// search_in_artefact result builder. When every match is in one fragment and the matches
/// share a single class, the result must hand back a ready apply_to_scope call (scope +
/// confirmed selector) instead of telling the agent to seek more context.
/// </summary>
public class ConversationStreamControllerDomSearchTests
{
    private static PrototypeDomSearchMatch Match(string fragmentPath, string nodeKey, IReadOnlyList<string> classList) =>
        new(
            NodeKey: nodeKey,
            FragmentPath: fragmentPath,
            TagName: "span",
            TextSnippet: "urgency",
            CssSelector: $"css:{nodeKey}",
            ClassList: classList,
            ParentContext: "div",
            SiblingContext: string.Empty);

    [Fact]
    public void BuildDomSearchMultiMatchResult_SameFragmentSharedClass_ReturnsReadyApplyToScopeCall()
    {
        const string fragment = "prototype/fragments/screen-01-legacy.html";
        var matches = Enumerable.Range(1, 10)
            .Select(index => Match(fragment, $"{fragment}|css:n{index}", ["urgency-arrow"]))
            .ToList();

        var result = ConversationStreamController.BuildDomSearchMultiMatchResult(
            "urgency", matches, confirmedSelector: ".urgency-arrow");

        Assert.Contains("apply_to_scope(scope=\"screen-01-legacy\", selector=\".urgency-arrow\"", result, StringComparison.Ordinal);
        Assert.DoesNotContain("Ask the user", result, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildDomSearchMultiMatchResult_MultipleFragments_ReturnsAskUser()
    {
        var matches = new List<PrototypeDomSearchMatch>
        {
            Match("prototype/fragments/screen-01-legacy.html", "screen-01-legacy.html|css:a", ["urgency-arrow"]),
            Match("prototype/fragments/screen-02-home.html", "screen-02-home.html|css:b", ["urgency-arrow"]),
        };

        var result = ConversationStreamController.BuildDomSearchMultiMatchResult(
            "urgency", matches, confirmedSelector: null);

        Assert.Contains("Ask the user", result, StringComparison.Ordinal);
        Assert.DoesNotContain("apply_to_scope(", result, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildDomSearchMultiMatchResult_SameFragmentNoSharedClass_ReturnsAskUser()
    {
        const string fragment = "prototype/fragments/screen-01-legacy.html";
        var matches = new List<PrototypeDomSearchMatch>
        {
            Match(fragment, $"{fragment}|css:a", ["nav-link"]),
            Match(fragment, $"{fragment}|css:b", ["chip"]),
        };

        var result = ConversationStreamController.BuildDomSearchMultiMatchResult(
            "urgency", matches, confirmedSelector: null);

        Assert.Contains("Ask the user", result, StringComparison.Ordinal);
        Assert.DoesNotContain("apply_to_scope(", result, StringComparison.Ordinal);
    }
}
