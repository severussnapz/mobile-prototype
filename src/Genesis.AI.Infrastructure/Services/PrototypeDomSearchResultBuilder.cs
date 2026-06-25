using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using Genesis.AI.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Genesis.AI.Infrastructure.Services;

internal static class PrototypeDomSearchResultBuilder
{
    private static readonly Regex NonLetterOnlyRegex = new(@"^\P{L}+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    internal sealed record SearchCandidate(
        PrototypeDomSearchMatch Match,
        bool IsLeaf,
        bool DirectTextContainsQuery,
        int ChildElementCount,
        int TextLength);

    internal static async Task<IReadOnlyList<SearchCandidate>> CollectSelectorMatchesAsync(
        IReadOnlyList<(string FragmentPath, string Content)> fragments,
        string query,
        ISet<string> excludedTags,
        CancellationToken cancellationToken)
    {
        using var browsingContext = AngleSharp.BrowsingContext.New(AngleSharp.Configuration.Default);
        var candidates = new List<SearchCandidate>();

        foreach (var (fragmentPath, fragmentHtml) in fragments)
        {
            using var document = await browsingContext.OpenAsync(response => response.Content(fragmentHtml), cancellationToken);
            var cssMatches = QueryWithSelectorVariants(document, query, excludedTags);
            candidates.AddRange(cssMatches.Select(element => CreateSearchCandidate(fragmentPath, element, query)));
        }

        return candidates;
    }

    internal static async Task<IReadOnlyList<SearchCandidate>> CollectClassTokenMatchesAsync(
        IReadOnlyList<(string FragmentPath, string Content)> fragments,
        string query,
        ISet<string> excludedTags,
        CancellationToken cancellationToken)
    {
        using var browsingContext = AngleSharp.BrowsingContext.New(AngleSharp.Configuration.Default);
        var tokens = PrototypeDomSearchHelper.TokeniseQuery(query);
        if (tokens.Count == 0)
        {
            return [];
        }

        var candidates = new List<SearchCandidate>();
        foreach (var (fragmentPath, fragmentHtml) in fragments)
        {
            using var document = await browsingContext.OpenAsync(response => response.Content(fragmentHtml), cancellationToken);
            var matchingClasses = PrototypeDomSearchDocumentHelper.CollectClassNames(document, excludedTags)
                .Where(className => PrototypeDomSearchHelper.ClassMatchesAllTokens(className, tokens));

            foreach (var className in matchingClasses)
            {
                var elements = document.QuerySelectorAll($".{className}")
                    .OfType<IElement>()
                    .Where(element => !excludedTags.Contains(element.TagName));
                candidates.AddRange(elements.Select(element => CreateSearchCandidate(fragmentPath, element, query)));
            }
        }

        return candidates;
    }

    internal static async Task<IReadOnlyList<SearchCandidate>> CollectTextMatchesAsync(
        IReadOnlyList<(string FragmentPath, string Content)> fragments,
        string query,
        ISet<string> excludedTags,
        CancellationToken cancellationToken)
    {
        using var browsingContext = AngleSharp.BrowsingContext.New(AngleSharp.Configuration.Default);
        var candidates = new List<SearchCandidate>();

        foreach (var (fragmentPath, fragmentHtml) in fragments)
        {
            using var document = await browsingContext.OpenAsync(response => response.Content(fragmentHtml), cancellationToken);

            var textMatches = document.All
                .OfType<IElement>()
                .Where(element =>
                    !excludedTags.Contains(element.TagName) &&
                    HasSearchableDirectText(element) &&
                    PrototypeDomSearchDocumentHelper.GetAllSearchableText(element)
                        .Contains(query, StringComparison.OrdinalIgnoreCase));

            candidates.AddRange(textMatches.Select(element => CreateSearchCandidate(fragmentPath, element, query)));
        }

        return candidates;
    }

    internal static PrototypeDomSearchResult BuildRankedResult(
        IReadOnlyList<SearchCandidate> matchedCandidates,
        Guid projectId,
        string query,
        int maxResultCount,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        var projectedMatches = matchedCandidates
            .OrderByDescending(candidate => candidate.IsLeaf)
            .ThenByDescending(candidate => candidate.DirectTextContainsQuery)
            .ThenBy(candidate => candidate.ChildElementCount)
            .ThenBy(candidate => candidate.TextLength)
            .Select(candidate => candidate.Match)
            .DistinctBy(candidate => candidate.NodeKey)
            .ToList();

        var totalMatches = projectedMatches.Count;
        var cappedMatches = projectedMatches.Take(maxResultCount).ToList();

        logger.LogInformation(
            "PrototypeDomSearchService search complete for project {ProjectId}: query={Query}, totalMatches={TotalMatches}, returned={ReturnedCount}",
            projectId, query, totalMatches, cappedMatches.Count);

        return new PrototypeDomSearchResult(
            Matches: cappedMatches,
            Truncated: totalMatches > maxResultCount,
            TotalMatches: totalMatches);
    }

    private static SearchCandidate CreateSearchCandidate(string fragmentPath, IElement element, string query)
    {
        return new SearchCandidate(
            Match: PrototypeDomSearchDocumentHelper.BuildSearchMatch(fragmentPath, element),
            IsLeaf: element.ChildElementCount == 0,
            DirectTextContainsQuery: PrototypeDomSearchDocumentHelper.DoesDirectTextContainQuery(element, query),
            ChildElementCount: element.ChildElementCount,
            TextLength: element.TextContent.Trim().Length);
    }

    internal static List<IElement> QueryWithSelectorVariants(IDocument document, string query, ISet<string> excludedTags)
    {
        var selectorCandidates = PrototypeDomSearchHelper.BuildSelectorCandidates(query);
        foreach (var selector in selectorCandidates)
        {
            List<IElement> matches;
            try
            {
                matches = document.QuerySelectorAll(selector)
                    .OfType<IElement>()
                    .Where(element => !excludedTags.Contains(element.TagName))
                    .ToList();
            }
            catch
            {
                continue;
            }

            if (matches.Count > 0) { return matches; }
        }

        return [];
    }

    internal static bool HasSearchableDirectText(IElement element)
    {
        var directText = string.Concat(
            element.ChildNodes
                .OfType<IText>()
                .Select(textNode => textNode.Data))
            .Trim();

        if (directText.Length < 3)
        {
            return false;
        }

        return !NonLetterOnlyRegex.IsMatch(directText);
    }
}