using System.Globalization;
using System.IO;
using AngleSharp;
using AngleSharp.Dom;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

internal static class PrototypeDomSearchHelper
{
    private const int MaxSnippetLength = 100;

    internal static List<string> BuildSelectorCandidates(string query)
    {
        var selectorCandidates = new List<string> { query };
        if (!query.StartsWith('.') && !query.StartsWith('#'))
        {
            selectorCandidates.Add($".{query}");
            selectorCandidates.Add($"#{query}");
        }

        return selectorCandidates;
    }

    private static readonly char[] QuerySeparators = [' ', '\t', '-', '_'];

    /// <summary>
    /// Splits a natural-language query into lowercase word tokens on whitespace, hyphens and
    /// underscores. "urgency arrow", "urgency-arrow" and "Urgency Arrow" all tokenise identically
    /// to ["urgency", "arrow"], so a spoken description can be matched against a kebab-case class.
    /// </summary>
    internal static IReadOnlyList<string> TokeniseQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        return query
            .Split(QuerySeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => token.ToLowerInvariant())
            .ToList();
    }

    /// <summary>
    /// True when every query token equals a hyphen/underscore segment of the class name. Uses
    /// segment equality (not raw substring) so "view" matches the segment "view" but not "review".
    /// Order-independent: "primary button" matches both "primary-button" and "button-primary".
    /// Abbreviated classes are intentionally not handled — "smart view" does not match "sv-item".
    /// </summary>
    internal static bool ClassMatchesAllTokens(string className, IReadOnlyList<string> tokens)
    {
        if (tokens.Count == 0 || string.IsNullOrWhiteSpace(className))
        {
            return false;
        }

        var segments = className
            .Split(QuerySeparators, StringSplitOptions.RemoveEmptyEntries)
            .Select(segment => segment.ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);

        return tokens.All(segments.Contains);
    }

    /// <summary>
    /// Finds elements whose class name matches every query token by segment equality. This bridges
    /// a natural-language query ("urgency arrow") to a kebab-case class (".urgency-arrow") without
    /// the caller knowing the class name in advance. Class names come from the parsed class list,
    /// so the derived ".{class}" selector is always a valid identifier.
    /// </summary>
    internal static async Task<IReadOnlyList<(string FragmentPath, IElement Element)>> SearchByClassTokensAsync(
        IBrowsingContext browsingContext,
        IReadOnlyList<(string FragmentPath, string Content)> fragments,
        string query,
        ISet<string> excludedTags,
        CancellationToken cancellationToken)
    {
        var tokens = TokeniseQuery(query);
        if (tokens.Count == 0)
        {
            return [];
        }

        var classTokenMatches = new List<(string FragmentPath, IElement Element)>();
        foreach (var (fragmentPath, fragmentHtml) in fragments)
        {
            using var document = await browsingContext.OpenAsync(
                response => response.Content(fragmentHtml), cancellationToken);

            var matchingClasses = CollectClassNames(document, excludedTags)
                .Where(className => ClassMatchesAllTokens(className, tokens));

            foreach (var className in matchingClasses)
            {
                var elements = document.QuerySelectorAll($".{className}")
                    .OfType<IElement>()
                    .Where(element => !excludedTags.Contains(element.TagName))
                    .Select(element => (fragmentPath, element));
                classTokenMatches.AddRange(elements);
            }
        }

        return classTokenMatches;
    }

    internal static IReadOnlyCollection<string> CollectClassNames(
        IDocument document,
        ISet<string> excludedTags)
    {
        var classNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var element in document.All
            .OfType<IElement>()
            .Where(element => !excludedTags.Contains(element.TagName)))
        {
            foreach (var className in element.ClassList)
            {
                classNames.Add(className);
            }
        }

        return classNames;
    }

    internal static List<PrototypeDomSearchMatch> BuildScopeMatches(
        IDocument document,
        string fragmentPath,
        ISet<string> excludedTags,
        int maxResultCount)
    {
        return document.All
            .OfType<IElement>()
            .Where(element => !excludedTags.Contains(element.TagName))
            .Select(element => BuildSearchMatch(fragmentPath, element))
            .DistinctBy(match => match.NodeKey)
            .Take(maxResultCount)
            .ToList();
    }

    internal static (string FragmentPath, string Content)? OpenFragmentByScope(
        IReadOnlyList<(string FragmentPath, string Content)> fragments,
        string scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            return null;
        }

        var targetFragment = fragments.FirstOrDefault(fragment =>
            Path.GetFileNameWithoutExtension(fragment.FragmentPath)
                .Equals(scope, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrEmpty(targetFragment.FragmentPath))
        {
            return null;
        }

        return (targetFragment.FragmentPath, targetFragment.Content);
    }

    internal static bool DoesDirectTextContainQuery(IElement element, string query)
    {
        var directText = string.Concat(
            element.ChildNodes
                .OfType<IText>()
                .Select(textNode => textNode.Data))
            .Trim();

        return !string.IsNullOrWhiteSpace(directText) &&
               directText.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    internal static string GetAllSearchableText(IElement element)
    {
        return string.Join(" ",
            new[]
            {
                element.TextContent,
                element.GetAttribute("title"),
                element.GetAttribute("aria-label"),
                element.GetAttribute("placeholder"),
                element.GetAttribute("alt"),
                element.GetAttribute("value"),
                element.GetAttribute("name"),
                element.GetAttribute("data-label"),
            }
            .Where(text => !string.IsNullOrWhiteSpace(text)));
    }

    internal static string GetSearchableAttributes(IElement element)
    {
        return string.Join(" ",
            new[]
            {
                element.GetAttribute("title"),
                element.GetAttribute("aria-label"),
                element.GetAttribute("placeholder"),
                element.GetAttribute("alt"),
                element.GetAttribute("value"),
                element.GetAttribute("name"),
                element.GetAttribute("data-label"),
            }
            .Where(text => !string.IsNullOrWhiteSpace(text)));
    }

    internal static PrototypeDomSearchMatch BuildSearchMatch(string fragmentPath, IElement element)
    {
        var cssSelector = element.GetSelector();
        var stableId = element.GetAttribute("data-genesis-id")
            ?? element.GetAttribute("id")
            ?? $"css:{cssSelector}";
        var nodeKey = string.Create(CultureInfo.InvariantCulture, $"{fragmentPath}|{stableId}");

        // Use direct text nodes only — not descendant text — so elements with child spans
        // (e.g. sv-item containing sv-item-label and sv-item-count) return only their own
        // whitespace-normalised text, not the concatenation of all child text.
        // This forces callers to target the specific child element for clean values.
        var directText = string.Concat(
            element.ChildNodes
                .OfType<IText>()
                .Select(textNode => textNode.Data))
            .Trim();

        // Fall back chain: cleaned direct text → aria-label/attributes → first meaningful child text
        // Covers icons, arrows, emoji spans, SVGs where visible content is not real text.
        var cleanedDirectText = DeriveFromTextContentStrategy.CleanTextSnippet(directText);
        string trimmedTextContent;
        if (!string.IsNullOrWhiteSpace(cleanedDirectText))
        {
            trimmedTextContent = cleanedDirectText;
        }
        else
        {
            var attributes = GetSearchableAttributes(element).Trim();
            if (!string.IsNullOrWhiteSpace(attributes))
            {
                trimmedTextContent = attributes;
            }
            else
            {
                // Last resort: first child element that has meaningful direct text after cleaning
                // (skip emoji-only or symbol-only children like icon spans)
                var firstChildText = element.Children
                    .OfType<IElement>()
                    .Select(child => DeriveFromTextContentStrategy.CleanTextSnippet(
                        string.Concat(child.ChildNodes.OfType<IText>().Select(textNode => textNode.Data)).Trim()))
                    .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text)) ?? string.Empty;
                trimmedTextContent = firstChildText;
            }
        }

        if (trimmedTextContent.Length > MaxSnippetLength)
        {
            trimmedTextContent = trimmedTextContent[..MaxSnippetLength];
        }

        return new PrototypeDomSearchMatch(
            NodeKey: nodeKey,
            FragmentPath: fragmentPath,
            TagName: element.TagName.ToLowerInvariant(),
            TextSnippet: trimmedTextContent,
            CssSelector: cssSelector,
            ClassList: element.ClassList.ToList(),
            ParentContext: BuildParentContext(element),
            SiblingContext: BuildSiblingContext(element));
    }

    internal static string BuildParentContext(IElement element)
    {
        var parentElement = element.ParentElement;
        if (parentElement is null) { return string.Empty; }
        var parentTag = parentElement.TagName.ToLowerInvariant();
        var parentClassName = parentElement.ClassName?.Trim() ?? string.Empty;
        return string.IsNullOrWhiteSpace(parentClassName)
            ? parentTag
            : $"{parentTag}.{parentClassName}";
    }

    internal static string BuildSiblingContext(IElement element)
    {
        var parentElement = element.ParentElement;
        if (parentElement is null) { return string.Empty; }
        var siblingSnippets = parentElement.Children
            .OfType<IElement>()
            .Where(sibling => !ReferenceEquals(sibling, element) &&
                              sibling.TagName.Equals(element.TagName, StringComparison.OrdinalIgnoreCase))
            .Select(sibling => GetAllSearchableText(sibling).Trim())
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Select(text => text.Length > 20 ? text[..20] : text)
            .Take(5)
            .ToList();
        return siblingSnippets.Count == 0 ? string.Empty : string.Join(" | ", siblingSnippets);
    }

    internal static string ToNfc(string value)
    {
        return value.Normalize(System.Text.NormalizationForm.FormC);
    }

    /// <summary>
    /// Returns the single class shared by every match that carries a class, or null when the
    /// matches do not collapse to exactly one shared class. Elements with no class are ignored
    /// so structural containers do not dilute detection. Note: elements with multiple classes can
    /// still overmatch when one class is broadly reused; this is acceptable as a guidance
    /// heuristic for LLM retry suggestions, not a guarantee of exact targeting.
    /// </summary>
    internal static string? FindSingleSharedClass(IReadOnlyList<PrototypeDomSearchMatch> matches)
    {
        var classedElements = matches
            .Where(match => match.ClassList.Count > 0)
            .Select(match => (IReadOnlyCollection<string>)match.ClassList)
            .ToList();

        if (classedElements.Count == 0)
        {
            return null;
        }

        var sharedClasses = classedElements
            .Aggregate((accumulated, next) => accumulated.Intersect(next).ToList())
            .ToList();

        return sharedClasses.Count == 1 ? sharedClasses[0] : null;
    }

}
