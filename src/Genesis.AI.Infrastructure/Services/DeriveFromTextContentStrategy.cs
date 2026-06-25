using System.Globalization;
using System.Text.RegularExpressions;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

public sealed class DeriveFromTextContentStrategy : IApplyToScopeStrategy
{
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly string[] ArrowPrefixes =
    [
        "◀", "▶", "►", "◄", "▲", "▼", "→", "←", "↑", "↓",
        "»", "«", "›", "‹", "▸", "◂", "▴", "▾"
    ];

    public Task<IReadOnlyList<ApplyToScopeValueResult>> DeriveValuesAsync(
        IReadOnlyList<PrototypeDomSearchMatch> matches,
        string? literalValue,
        CancellationToken cancellationToken)
    {
        var results = matches
            .Select(match => new ApplyToScopeValueResult(
                NodeKey: match.NodeKey,
                FragmentPath: match.FragmentPath,
                Value: CleanTextSnippet(match.TextSnippet)))
            .ToList();

        return Task.FromResult<IReadOnlyList<ApplyToScopeValueResult>>(results);
    }

    internal static string CleanTextSnippet(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Normalise whitespace — replace newlines and multiple spaces with a single space.
        // Regression fix: elements with child spans produce text content with newlines between spans
        // e.g. "All documents\n27" which breaks HTML attribute values.
        var cleaned = WhitespaceRegex
            .Replace(text.Trim(), " ")
            .Trim();

        // Strip leading emoji (Unicode ranges)
        cleaned = StripLeadingEmoji(cleaned);

        // Strip leading arrow prefixes
        foreach (var arrow in ArrowPrefixes)
        {
            if (cleaned.StartsWith(arrow, StringComparison.Ordinal))
            {
                cleaned = cleaned[arrow.Length..].TrimStart();
                break;
            }
        }

        // Deduplicate repeated words (e.g. "Save Save" → "Save")
        cleaned = DeduplicateText(cleaned);

        return cleaned.Trim();
    }

    private static string StripLeadingEmoji(string text)
    {
        var enumerator = StringInfo.GetTextElementEnumerator(text);
        var firstElement = string.Empty;

        if (enumerator.MoveNext())
        {
            firstElement = enumerator.GetTextElement();
        }

        if (string.IsNullOrEmpty(firstElement))
            return text;

        // Check if first text element is an emoji (outside basic ASCII/Latin range)
        var codepoint = char.ConvertToUtf32(firstElement, 0);
        var isEmoji = codepoint > 0x2000;

        if (isEmoji)
            return text[firstElement.Length..].TrimStart();

        return text;
    }

    private static string DeduplicateText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 2)
            return text;

        // Check if first half equals second half (e.g. "Save Save")
        if (words.Length % 2 == 0)
        {
            var half = words.Length / 2;
            var firstHalf = string.Join(' ', words[..half]);
            var secondHalf = string.Join(' ', words[half..]);
            if (string.Equals(firstHalf, secondHalf, StringComparison.OrdinalIgnoreCase))
                return firstHalf;
        }

        return text;
    }
}
