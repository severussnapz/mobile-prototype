using System.Text;
using AngleSharp;
using AngleSharp.Dom;
using Genesis.AI.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Genesis.AI.Infrastructure.Services;

public sealed class PrototypeDomSearchService : IPrototypeDomSearchService
{
    private static readonly HashSet<string> ExcludedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "html", "head", "body", "script", "style", "meta", "link", "title"
    };

    private readonly ILogger<PrototypeDomSearchService> _logger;
    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;

    private const string PrototypeFragmentsPrefix = "prototype/fragments/";
    private const int MaxResultCount = 10;

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
            return new PrototypeDomSearchResult(Matches: [], Truncated: false, TotalMatches: 0);
        }

        var prototypeFragments = await LoadPrototypeFragmentsAsync(request.ProjectId, cancellationToken);
        if (prototypeFragments.Count == 0)
        {
            return new PrototypeDomSearchResult(Matches: [], Truncated: false, TotalMatches: 0);
        }

        var browsingContext = BrowsingContext.New(AngleSharp.Configuration.Default);
        var query = request.Query.Trim();
        var matchedElements = new List<(string FragmentPath, IElement Element)>();

        foreach (var (fragmentPath, fragmentHtml) in prototypeFragments)
        {
            var document = await browsingContext.OpenAsync(
                response => response.Content(fragmentHtml), cancellationToken);
            var cssMatches = QueryWithSelectorVariants(document, query);
            if (cssMatches.Count > 0)
            {
                matchedElements.AddRange(cssMatches.Select(element => (fragmentPath, element)));
            }
        }

        if (matchedElements.Count == 0)
        {
            var fallbackMatches = await SearchByTextContentAsync(
                browsingContext, prototypeFragments, query, cancellationToken);
            matchedElements.AddRange(fallbackMatches);
        }

        var rankedElements = matchedElements
            .OrderByDescending(match => match.Element.ChildElementCount == 0)
            .ThenByDescending(match => PrototypeDomSearchHelper.DoesDirectTextContainQuery(match.Element, query))
            .ThenBy(match => match.Element.ChildElementCount)
            .ThenBy(match => match.Element.TextContent.Trim().Length)
            .ToList();

        var projectedMatches = rankedElements
            .Select(match => PrototypeDomSearchHelper.BuildSearchMatch(match.FragmentPath, match.Element))
            .DistinctBy(match => match.NodeKey)
            .ToList();

        var totalMatches = projectedMatches.Count;
        var cappedMatches = projectedMatches.Take(MaxResultCount).ToList();

        _logger.LogInformation(
            "PrototypeDomSearchService search complete for project {ProjectId}: query={Query}, totalMatches={TotalMatches}, returned={ReturnedCount}",
            request.ProjectId, query, totalMatches, cappedMatches.Count);

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
            return new PrototypeDomSearchResult(Matches: [], Truncated: false, TotalMatches: 0);
        }

        var prototypeFragments = await LoadPrototypeFragmentsAsync(request.ProjectId, cancellationToken);
        if (prototypeFragments.Count == 0)
        {
            return new PrototypeDomSearchResult(Matches: [], Truncated: false, TotalMatches: 0);
        }

        var browsingContext = BrowsingContext.New(AngleSharp.Configuration.Default);
        var matchedElements = new List<(string FragmentPath, IElement Element)>();

        foreach (var (fragmentPath, fragmentHtml) in prototypeFragments)
        {
            var document = await browsingContext.OpenAsync(
                response => response.Content(fragmentHtml), cancellationToken);

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
                _logger.LogDebug(exception,
                    "PrototypeDomSearchService ListAllAsync: CSS selector parse failed for selector {Selector}",
                    request.Selector);
                continue;
            }

            matchedElements.AddRange(cssMatches.Select(element => (fragmentPath, element)));
        }

        var allMatches = matchedElements
            .Select(match => PrototypeDomSearchHelper.BuildSearchMatch(match.FragmentPath, match.Element))
            .DistinctBy(match => match.NodeKey)
            .ToList();

        _logger.LogInformation(
            "PrototypeDomSearchService ListAllAsync for project {ProjectId}: selector={Selector}, scopeNodeId={ScopeNodeId}, totalMatches={TotalMatches}",
            request.ProjectId, request.Selector, request.ScopeNodeId ?? "(none)", allMatches.Count);

        return new PrototypeDomSearchResult(
            Matches: allMatches,
            Truncated: false,
            TotalMatches: allMatches.Count);
    }

    private static IElement? ResolveNodeByKey(IDocument document, string? nodeKey, string fragmentPath)
    {
        if (string.IsNullOrWhiteSpace(nodeKey)) { return null; }

        var stableLocator = PrototypeDomSearchHelper.ExtractStableLocator(nodeKey, fragmentPath, out var fragmentMatches);
        if (!fragmentMatches || string.IsNullOrWhiteSpace(stableLocator)) { return null; }

        var dataGenesisMatch = document.QuerySelector(
            $"[data-genesis-id=\"{PrototypeDomSearchHelper.EscapeCssString(stableLocator)}\"]");
        if (dataGenesisMatch is not null) { return dataGenesisMatch; }

        if (stableLocator.Length > 0 && !char.IsDigit(stableLocator[0]))
        {
            var idMatch = document.QuerySelector($"#{PrototypeDomSearchHelper.EscapeCssIdentifier(stableLocator)}");
            if (idMatch is not null) { return idMatch; }
        }

        try { return document.QuerySelector(stableLocator); }
        catch { return null; }
    }

    private async Task<IReadOnlyList<(string FragmentPath, string Content)>> LoadPrototypeFragmentsAsync(
        Guid projectId, CancellationToken cancellationToken)
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
            if (content is null) { continue; }
            loadedFragments.Add((artefact.FilePath, PrototypeDomSearchHelper.ToNfc(content)));
        }

        return loadedFragments;
    }

    private static async Task<IReadOnlyList<(string FragmentPath, IElement Element)>> SearchByTextContentAsync(
        IBrowsingContext browsingContext,
        IReadOnlyList<(string FragmentPath, string Content)> fragments,
        string query,
        CancellationToken cancellationToken)
    {
        var fallbackMatches = new List<(string FragmentPath, IElement Element)>();
        foreach (var (fragmentPath, fragmentHtml) in fragments)
        {
            var document = await browsingContext.OpenAsync(
                response => response.Content(fragmentHtml), cancellationToken);

            var textMatches = document.All
                .OfType<IElement>()
                .Where(element =>
                    !ExcludedTags.Contains(element.TagName) &&
                    PrototypeDomSearchHelper.GetAllSearchableText(element)
                        .Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(element => (fragmentPath, element));

            fallbackMatches.AddRange(textMatches);
        }

        return fallbackMatches;
    }

    private List<IElement> QueryWithSelectorVariants(IDocument document, string query)
    {
        var selectorCandidates = PrototypeDomSearchHelper.BuildSelectorCandidates(query);
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
                _logger.LogDebug(exception,
                    "Prototype DOM CSS selector parse failed for selector {Selector} from query {Query}",
                    selector, query);
                continue;
            }

            if (matches.Count > 0) { return matches; }
        }

        return [];
    }

    private async Task<(string FragmentPath, IDocument Document)?> OpenScopeFragmentAsync(
        Guid projectId,
        string scope,
        CancellationToken cancellationToken)
    {
        var prototypeFragments = await LoadPrototypeFragmentsAsync(projectId, cancellationToken);
        return await PrototypeDomSearchHelper.OpenFragmentByScopeAsync(prototypeFragments, scope, cancellationToken);
    }

    public async Task<PrototypeDomSearchResult> ListAllInScopeAsync(
        Guid projectId,
        string scope,
        CancellationToken cancellationToken)
    {
        var resolved = await OpenScopeFragmentAsync(projectId, scope, cancellationToken);
        if (resolved is null)
        {
            return new PrototypeDomSearchResult(Matches: [], Truncated: false, TotalMatches: 0);
        }

        var (fragmentPath, document) = resolved.Value;
        var elements = PrototypeDomSearchHelper.BuildScopeMatches(document, fragmentPath, ExcludedTags, MaxResultCount);

        _logger.LogInformation(
            "PrototypeDomSearchService ListAllInScopeAsync for project {ProjectId}: scope={Scope}, elementsFound={Count}", projectId, scope, elements.Count);

        return new PrototypeDomSearchResult(
            Matches: elements,
            Truncated: false,
            TotalMatches: elements.Count);
    }

    public async Task<IReadOnlyCollection<string>> GetClassNamesInScopeAsync(
        Guid projectId,
        string scope,
        CancellationToken cancellationToken)
    {
        var resolved = await OpenScopeFragmentAsync(projectId, scope, cancellationToken);
        if (resolved is null)
        {
            return [];
        }

        var classNames = PrototypeDomSearchHelper.CollectClassNames(resolved.Value.Document, ExcludedTags);
        _logger.LogInformation("GetClassNamesInScopeAsync: scope={Scope} classCount={Count}", scope, classNames.Count);
        return classNames;
    }


    public async Task<string?> ResolveConfirmedSelectorForScope(
        Guid projectId,
        string scope,
        CancellationToken cancellationToken)
    {
        var elements = await ListAllInScopeAsync(projectId, scope, cancellationToken);
        if (elements.Matches.Count == 0)
        {
            return null;
        }

        var sharedClass = PrototypeDomSearchHelper.FindSingleSharedClass(elements.Matches);
        return sharedClass is null ? null : $".{sharedClass}";
    }

}
