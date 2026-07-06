using System.Globalization;

namespace Genesis.AI.Infrastructure.Services;

internal static class PrototypeDomSearchHelper
{
    private static readonly char[] QuerySeparators = [' ', '\t', '-', '_'];

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
}
