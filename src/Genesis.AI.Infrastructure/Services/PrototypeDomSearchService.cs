using System.Globalization;
using System.Text;
using AngleSharp;
using AngleSharp.Dom;
using Genesis.AI.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Genesis.AI.Infrastructure.Services;

public sealed class PrototypeDomSearchService : IPrototypeDomSearchService
{
    private static readonly HashSet<string> ExcludedTags = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "html", "head", "body", "script", "style",
        "meta", "link", "title"
    };

    private readonly ILogger<PrototypeDomSearchService> _logger;
    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;

    private const string PrototypeFragmentsPrefix = "prototype/fragments/";
    private const int MaxResultCount = 10;
    private const int MaxSnippetLength = 100;

    public PrototypeDomSearchService(
        ILogger<PrototypeDomSearchService> logger,
        IArtefactRepository artefactRepository,
        IArtefactStorageService artefactStorageService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _artefactStorageService = artefactStorageService ?? throw new ArgumentNullException(nameof(artefactStorageService));
    }

    public async Task<PrototypeDomSearchResult> SearchAsync(
        PrototypeDomSearchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return new PrototypeDomSearchResult(
                Matches: [],
                Truncated: false,
                TotalMatches: 0);
        }

        var prototypeFragments = await LoadPrototypeFragmentsAsync(request.ProjectId, cancellationToken);
        if (prototypeFragments.Count == 0)
        {
            return new PrototypeDomSearchResult(
                Matches: [],
                Truncated: false,
                TotalMatches: 0);
        }

        var config = AngleSharp.Configuration.Default;
        var browsingContext = BrowsingContext.New(config);
        var query = request.Query.Trim();
        var matchedElements = new List<(string FragmentPath, IElement Element)>();

        foreach (var (fragmentPath, fragmentHtml) in prototypeFragments)
        {
            var document = await browsingContext.OpenAsync(
                response => response.Content(fragmentHtml),
                cancellationToken);

            var cssMatches = QueryWithSelectorVariants(document, query);
            if (cssMatches.Count > 0)
            {
                matchedElements.AddRange(cssMatches.Select(element => (fragmentPath, element)));
            }
        }

        if (matchedElements.Count == 0)
        {
            var fallbackMatches = await SearchByTextContentAsync(
                browsingContext,
                prototypeFragments,
                query,
                cancellationToken);
            matchedElements.AddRange(fallbackMatches);
        }

        var rankedElements = matchedElements
            .OrderByDescending(match => match.Element.ChildElementCount == 0)
            .ThenByDescending(match => DoesDirectTextContainQuery(match.Element, query))
            .ThenBy(match => match.Element.ChildElementCount)
            .ThenBy(match => match.Element.TextContent.Trim().Length)
            .ToList();

        var projectedMatches = rankedElements
            .Select(match => BuildSearchMatch(match.FragmentPath, match.Element))
            .DistinctBy(match => match.NodeKey)
            .ToList();

        var totalMatches = projectedMatches.Count;
        var cappedMatches = projectedMatches
            .Take(MaxResultCount)
            .ToList();

        _logger.LogInformation(
            "PrototypeDomSearchService search complete for project {ProjectId}, file {FilePath}: query={Query}, totalMatches={TotalMatches}, returned={ReturnedCount}",
            request.ProjectId,
            request.FilePath,
            query,
            totalMatches,
            cappedMatches.Count);

        return new PrototypeDomSearchResult(
            Matches: cappedMatches,
            Truncated: totalMatches > MaxResultCount,
            TotalMatches: totalMatches);
    }

    public async Task<PrototypeDomSearchResult> ListAllAsync(
        PrototypeDomListRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Selector))
        {
            return new PrototypeDomSearchResult(
                Matches: [],
                Truncated: false,
                TotalMatches: 0);
        }

        var prototypeFragments = await LoadPrototypeFragmentsAsync(request.ProjectId, cancellationToken);
        if (prototypeFragments.Count == 0)
        {
            return new PrototypeDomSearchResult(
                Matches: [],
                Truncated: false,
                TotalMatches: 0);
        }

        var config = AngleSharp.Configuration.Default;
        var browsingContext = BrowsingContext.New(config);
        var matchedElements = new List<(string FragmentPath, IElement Element)>();

        foreach (var (fragmentPath, fragmentHtml) in prototypeFragments)
        {
            var document = await browsingContext.OpenAsync(
                response => response.Content(fragmentHtml),
                cancellationToken);

            List<IElement> cssMatches;
            try
            {
                var scopeElement = ResolveNodeByKey(document, request.ScopeNodeId, fragmentPath);
                if (!string.IsNullOrWhiteSpace(request.ScopeNodeId) && scopeElement is null)
                {
                    continue;
                }

                cssMatches = (scopeElement?.QuerySelectorAll(request.Selector) ?? document.QuerySelectorAll(request.Selector))
                    .OfType<IElement>()
                    .Where(element => !ExcludedTags.Contains(element.TagName))
                    .ToList();
            }
            catch (Exception exception)
            {
                _logger.LogDebug(
                    exception,
                    "PrototypeDomSearchService ListAllAsync: CSS selector parse failed for selector {Selector}",
                    request.Selector);
                continue;
            }

            matchedElements.AddRange(cssMatches.Select(element => (fragmentPath, element)));
        }

        var allMatches = matchedElements
            .Select(match => BuildSearchMatch(match.FragmentPath, match.Element))
            .DistinctBy(match => match.NodeKey)
            .ToList();

        _logger.LogInformation(
            "PrototypeDomSearchService ListAllAsync for project {ProjectId}: selector={Selector}, scopeNodeId={ScopeNodeId}, totalMatches={TotalMatches}",
            request.ProjectId,
            request.Selector,
            request.ScopeNodeId ?? "(none)",
            allMatches.Count);

        return new PrototypeDomSearchResult(
            Matches: allMatches,
            Truncated: false,
            TotalMatches: allMatches.Count);
    }

    private static IElement? ResolveNodeByKey(IDocument document, string? nodeKey, string fragmentPath)
    {
        if (string.IsNullOrWhiteSpace(nodeKey))
        {
            return null;
        }

        var stableLocator = ExtractStableLocator(nodeKey, fragmentPath, out var fragmentMatches);
        if (!fragmentMatches || string.IsNullOrWhiteSpace(stableLocator))
        {
            return null;
        }

        var dataGenesisSelector = $"[data-genesis-id=\"{EscapeCssString(stableLocator)}\"]";
        var dataGenesisMatch = document.QuerySelector(dataGenesisSelector);
        if (dataGenesisMatch is not null)
        {
            return dataGenesisMatch;
        }

        if (stableLocator.Length > 0 && !char.IsDigit(stableLocator[0]))
        {
            var idSelector = $"#{EscapeCssIdentifier(stableLocator)}";
            var idMatch = document.QuerySelector(idSelector);
            if (idMatch is not null)
            {
                return idMatch;
            }
        }

        try
        {
            return document.QuerySelector(stableLocator);
        }
        catch
        {
            return null;
        }
    }

    private static string ExtractStableLocator(string nodeKey, string fragmentPath, out bool fragmentMatches)
    {
        fragmentMatches = true;

        var separatorIndex = nodeKey.IndexOf('|', StringComparison.Ordinal);
        if (separatorIndex < 0)
        {
            return nodeKey.Trim();
        }

        var nodeKeyFragmentPath = nodeKey[..separatorIndex];
        fragmentMatches = nodeKeyFragmentPath.Equals(fragmentPath, StringComparison.OrdinalIgnoreCase);
        return nodeKey[(separatorIndex + 1)..].Trim();
    }

    private static string EscapeCssString(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    private static string EscapeCssIdentifier(string value)
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

    private async Task<IReadOnlyList<(string FragmentPath, string Content)>> LoadPrototypeFragmentsAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var allArtefacts = await _artefactRepository.GetByProjectIdAsync(projectId, cancellationToken);
        var latestFragmentArtefacts = allArtefacts
            .Where(artefact =>
                artefact.FilePath.StartsWith(PrototypeFragmentsPrefix, StringComparison.OrdinalIgnoreCase) &&
                artefact.FilePath.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            .GroupBy(artefact => artefact.FilePath, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(artefact => artefact.Version).First())
            .OrderBy(artefact => artefact.FilePath, StringComparer.Ordinal)
            .ToList();

        var loadedFragments = new List<(string FragmentPath, string Content)>();
        foreach (var artefact in latestFragmentArtefacts)
        {
            var content = await _artefactStorageService.GetContentAsync(artefact.S3Key, cancellationToken);
            if (content is null)
            {
                continue;
            }

            loadedFragments.Add((artefact.FilePath, ToNfc(content)));
        }

        return loadedFragments;
    }

    private async Task<IReadOnlyList<(string FragmentPath, IElement Element)>> SearchByTextContentAsync(
        IBrowsingContext browsingContext,
        IReadOnlyList<(string FragmentPath, string Content)> fragments,
        string query,
        CancellationToken cancellationToken)
    {
        var fallbackMatches = new List<(string FragmentPath, IElement Element)>();
        var debugLogCount = 0;

        foreach (var (fragmentPath, fragmentHtml) in fragments)
        {
            var document = await browsingContext.OpenAsync(
                response => response.Content(fragmentHtml),
                cancellationToken);

            var textMatches = document.All
                .OfType<IElement>()
                .Where(element =>
                    !ExcludedTags.Contains(element.TagName) &&
                    GetAllSearchableText(element).Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(element => (fragmentPath, element));

            foreach (var (_, element) in textMatches)
            {
                if (query.Equals("New Smart View", StringComparison.OrdinalIgnoreCase) && debugLogCount < 5)
                {
                    var searchableText = GetAllSearchableText(element);
                    _logger.LogDebug("Text search candidate: tag={Tag} text='{Text}'",
                        element.TagName, searchableText[..Math.Min(50, searchableText.Length)]);
                    debugLogCount++;
                }
            }

            fallbackMatches.AddRange(textMatches);
        }

        return fallbackMatches;
    }

    private static string GetAllSearchableText(IElement element)
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

    private static PrototypeDomSearchMatch BuildSearchMatch(string fragmentPath, IElement element)
    {
        var cssSelector = element.GetSelector();
        var stableId = element.GetAttribute("data-genesis-id")
            ?? element.GetAttribute("id")
            ?? $"css:{cssSelector}";
        var nodeKey = string.Create(
            CultureInfo.InvariantCulture,
            $"{fragmentPath}|{stableId}");
        var trimmedTextContent = GetAllSearchableText(element).Trim();

        if (trimmedTextContent.Length > MaxSnippetLength)
        {
            trimmedTextContent = trimmedTextContent[..MaxSnippetLength];
        }

        var parentContext = BuildParentContext(element);
        var siblingContext = BuildSiblingContext(element);

        return new PrototypeDomSearchMatch(
            NodeKey: nodeKey,
            FragmentPath: fragmentPath,
            TagName: element.TagName.ToLowerInvariant(),
            TextSnippet: trimmedTextContent,
            CssSelector: cssSelector,
            ClassList: element.ClassList.ToList(),
            ParentContext: parentContext,
            SiblingContext: siblingContext);
    }

    private static string BuildParentContext(IElement element)
    {
        var parentElement = element.ParentElement;
        if (parentElement is null)
        {
            return string.Empty;
        }

        var parentTag = parentElement.TagName.ToLowerInvariant();
        var parentClassName = parentElement.ClassName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(parentClassName))
        {
            return parentTag;
        }

        return $"{parentTag}.{parentClassName}";
    }

    private static string BuildSiblingContext(IElement element)
    {
        var parentElement = element.ParentElement;
        if (parentElement is null)
        {
            return string.Empty;
        }

        var siblingSnippets = parentElement.Children
            .OfType<IElement>()
            .Where(sibling => !ReferenceEquals(sibling, element) &&
                              sibling.TagName.Equals(element.TagName, StringComparison.OrdinalIgnoreCase))
            .Select(sibling => GetAllSearchableText(sibling).Trim())
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Select(text => text.Length > 20 ? text[..20] : text)
            .Take(5)
            .ToList();

        return siblingSnippets.Count == 0
            ? string.Empty
            : string.Join(" | ", siblingSnippets);
    }

    private List<IElement> QueryWithSelectorVariants(IDocument document, string query)
    {
        var selectorCandidates = BuildSelectorCandidates(query);
        foreach (var selector in selectorCandidates)
        {
            List<IElement> matches;
            try
            {
                matches = document.QuerySelectorAll(selector)
                    .OfType<IElement>()
                    .Where(element => !ExcludedTags.Contains(element.TagName))
                    .ToList();
            }
            catch (Exception exception)
            {
                _logger.LogDebug(
                    exception,
                    "Prototype DOM CSS selector parse failed for selector {Selector} from query {Query}",
                    selector,
                    query);
                continue;
            }

            if (matches.Count > 0)
            {
                return matches;
            }
        }

        return [];
    }

    private static List<string> BuildSelectorCandidates(string query)
    {
        var selectorCandidates = new List<string> { query };

        if (!query.StartsWith('.') &&
            !query.StartsWith('#'))
        {
            selectorCandidates.Add($".{query}");
            selectorCandidates.Add($"#{query}");
        }

        return selectorCandidates;
    }

    private static bool DoesDirectTextContainQuery(IElement element, string query)
    {
        var directText = string.Concat(
            element.ChildNodes
                .OfType<IText>()
                .Select(textNode => textNode.Data))
            .Trim();

        return !string.IsNullOrWhiteSpace(directText) &&
               directText.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private static string ToNfc(string value)
    {
        return value.Normalize(NormalizationForm.FormC);
    }
}
