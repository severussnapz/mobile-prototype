using System.Globalization;
using System.Text;
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

    internal static async Task<(string FragmentPath, IDocument Document)?> OpenFragmentByScopeAsync(
        IReadOnlyList<(string FragmentPath, string Content)> fragments,
        string scope,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            return null;
        }

        var targetFragment = fragments.FirstOrDefault(fragment =>
            System.IO.Path.GetFileNameWithoutExtension(fragment.FragmentPath)
                .Equals(scope, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrEmpty(targetFragment.FragmentPath))
        {
            return null;
        }

        var browsingContext = BrowsingContext.New(AngleSharp.Configuration.Default);
        var document = await browsingContext.OpenAsync(
            response => response.Content(targetFragment.Content), cancellationToken);

        return (targetFragment.FragmentPath, document);
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
                .OfType<AngleSharp.Dom.IText>()
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
                        string.Concat(child.ChildNodes.OfType<AngleSharp.Dom.IText>().Select(textNode => textNode.Data)).Trim()))
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

    internal static string ExtractStableLocator(string nodeKey, string fragmentPath, out bool fragmentMatches)
    {
        fragmentMatches = true;
        var separatorIndex = nodeKey.IndexOf('|', StringComparison.Ordinal);
        if (separatorIndex < 0) { return nodeKey.Trim(); }
        var nodeKeyFragmentPath = nodeKey[..separatorIndex];
        fragmentMatches = nodeKeyFragmentPath.Equals(fragmentPath, StringComparison.OrdinalIgnoreCase);
        return nodeKey[(separatorIndex + 1)..].Trim();
    }

    internal static string EscapeCssString(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    internal static string EscapeCssIdentifier(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character) || character is '-' or '_')
            {
                builder.Append(character);
                continue;
            }

            builder.Append('\\');
            builder.Append(character);
        }

        return builder.ToString();
    }

    internal static string ToNfc(string value)
    {
        return value.Normalize(System.Text.NormalizationForm.FormC);
    }

    /// <summary>
    /// Returns the single class shared by every match that carries a class, or null when the
    /// matches do not collapse to exactly one shared class. Elements with no class are ignored
    /// so structural containers do not dilute detection. Pure and domain-agnostic.
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
