using System.Globalization;
using System.Text;
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

    internal static PrototypeDomSearchMatch BuildSearchMatch(string fragmentPath, IElement element)
    {
        var cssSelector = element.GetSelector();
        var stableId = element.GetAttribute("data-genesis-id")
            ?? element.GetAttribute("id")
            ?? $"css:{cssSelector}";
        var nodeKey = string.Create(CultureInfo.InvariantCulture, $"{fragmentPath}|{stableId}");
        var trimmedTextContent = GetAllSearchableText(element).Trim();
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
