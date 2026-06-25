using System.Text;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using Genesis.AI.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Genesis.AI.Infrastructure.Services;

public sealed class PrototypeDomSearchService : IPrototypeDomSearchService
{
    private sealed record SearchCandidate(
        PrototypeDomSearchMatch Match,
        bool IsLeaf,
        bool DirectTextContainsQuery,
        int ChildElementCount,
        int TextLength);

    private static readonly HashSet<string> ExcludedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "html", "head", "body", "script", "style", "meta", "link", "title"
    };

    private readonly ILogger<PrototypeDomSearchService> _logger;
    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;

    private const string PrototypeFragmentsPrefix = "prototype/fragments/";
    private const int MaxResultCount = 10;
    private static readonly Regex NonLetterOnlyRegex = new(@"^\P{L}+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

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

        using var browsingContext = BrowsingContext.New(AngleSharp.Configuration.Default);
        var query = request.Query.Trim();
        var matchedCandidates = new List<SearchCandidate>();

        foreach (var (fragmentPath, fragmentHtml) in prototypeFragments)
        {
            using var document = await browsingContext.OpenAsync(
                response => response.Content(fragmentHtml), cancellationToken);
            var cssMatches = QueryWithSelectorVariants(document, query);
            if (cssMatches.Count > 0)
            {
                matchedCandidates.AddRange(cssMatches.Select(element => CreateSearchCandidate(fragmentPath, element, query)));
            }
        }

        if (matchedCandidates.Count == 0)
        {
            var tokens = PrototypeDomSearchHelper.TokeniseQuery(query);
            if (tokens.Count > 0)
            {
                foreach (var (fragmentPath, fragmentHtml) in prototypeFragments)
                {
                    using var document = await browsingContext.OpenAsync(
                        response => response.Content(fragmentHtml), cancellationToken);

                    var matchingClasses = PrototypeDomSearchHelper.CollectClassNames(document, ExcludedTags)
                        .Where(className => PrototypeDomSearchHelper.ClassMatchesAllTokens(className, tokens));

                    foreach (var className in matchingClasses)
                    {
                        var elements = document.QuerySelectorAll($".{className}")
                            .OfType<IElement>()
                            .Where(element => !ExcludedTags.Contains(element.TagName));
                        matchedCandidates.AddRange(elements.Select(element => CreateSearchCandidate(fragmentPath, element, query)));
                    }
                }
            }
        }

        if (matchedCandidates.Count == 0)
        {
            foreach (var (fragmentPath, fragmentHtml) in prototypeFragments)
            {
                using var document = await browsingContext.OpenAsync(
                    response => response.Content(fragmentHtml), cancellationToken);

                var textMatches = document.All
                    .OfType<IElement>()
                    .Where(element =>
                        !ExcludedTags.Contains(element.TagName) &&
                        HasSearchableDirectText(element) &&
                        PrototypeDomSearchHelper.GetAllSearchableText(element)
                            .Contains(query, StringComparison.OrdinalIgnoreCase));

                matchedCandidates.AddRange(textMatches.Select(element => CreateSearchCandidate(fragmentPath, element, query)));
            }
        }

        return BuildRankedResult(matchedCandidates, request.ProjectId, query);
    }

    private PrototypeDomSearchResult BuildRankedResult(
        IReadOnlyList<SearchCandidate> matchedCandidates,
        Guid projectId,
        string query)
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
        var cappedMatches = projectedMatches.Take(MaxResultCount).ToList();

        _logger.LogInformation(
            "PrototypeDomSearchService search complete for project {ProjectId}: query={Query}, totalMatches={TotalMatches}, returned={ReturnedCount}",
            projectId, query, totalMatches, cappedMatches.Count);

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
        var resolved = PrototypeDomSearchHelper.OpenFragmentByScope(
            prototypeFragments, request.Scope);
        if (resolved is null)
        {
            return new PrototypeDomSearchResult(Matches: [], Truncated: false, TotalMatches: 0);
        }

        var (fragmentPath, fragmentHtml) = resolved.Value;
        using var browsingContext = BrowsingContext.New(AngleSharp.Configuration.Default);
        using var document = await browsingContext.OpenAsync(
            response => response.Content(fragmentHtml), cancellationToken);

        List<PrototypeDomSearchMatch> allMatches;
        try
        {
            allMatches = document.QuerySelectorAll(request.Selector)
                .OfType<IElement>()
                .Where(element => !ExcludedTags.Contains(element.TagName))
                .Select(element => PrototypeDomSearchHelper.BuildSearchMatch(fragmentPath, element))
                .DistinctBy(match => match.NodeKey)
                .ToList();
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception,
                "PrototypeDomSearchService ListAllAsync: CSS selector parse failed for selector {Selector}", request.Selector);
            return new PrototypeDomSearchResult(Matches: [], Truncated: false, TotalMatches: 0);
        }

        _logger.LogInformation(
            "PrototypeDomSearchService ListAllAsync for project {ProjectId}: scope={Scope}, selector={Selector}, totalMatches={TotalMatches}",
            request.ProjectId, request.Scope, request.Selector, allMatches.Count);

        return new PrototypeDomSearchResult(
            Matches: allMatches,
            Truncated: false,
            TotalMatches: allMatches.Count);
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

    private static SearchCandidate CreateSearchCandidate(string fragmentPath, IElement element, string query)
    {
        return new SearchCandidate(
            Match: PrototypeDomSearchHelper.BuildSearchMatch(fragmentPath, element),
            IsLeaf: element.ChildElementCount == 0,
            DirectTextContainsQuery: PrototypeDomSearchHelper.DoesDirectTextContainQuery(element, query),
            ChildElementCount: element.ChildElementCount,
            TextLength: element.TextContent.Trim().Length);
    }

    private static bool HasSearchableDirectText(IElement element)
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

    private async Task<(string FragmentPath, string Content)?> OpenScopeFragmentAsync(
        Guid projectId,
        string scope,
        CancellationToken cancellationToken)
    {
        var prototypeFragments = await LoadPrototypeFragmentsAsync(projectId, cancellationToken);
        return PrototypeDomSearchHelper.OpenFragmentByScope(prototypeFragments, scope);
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

        var (fragmentPath, fragmentHtml) = resolved.Value;
        using var browsingContext = BrowsingContext.New(AngleSharp.Configuration.Default);
        using var document = await browsingContext.OpenAsync(
            response => response.Content(fragmentHtml), cancellationToken);
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

        var (_, fragmentHtml) = resolved.Value;
        using var browsingContext = BrowsingContext.New(AngleSharp.Configuration.Default);
        using var document = await browsingContext.OpenAsync(
            response => response.Content(fragmentHtml), cancellationToken);
        var classNames = PrototypeDomSearchHelper.CollectClassNames(document, ExcludedTags);
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

    public string? ResolveConfirmedSelectorFromMatches(IReadOnlyList<PrototypeDomSearchMatch> matches)
    {
        ArgumentNullException.ThrowIfNull(matches);
        var sharedClass = PrototypeDomSearchHelper.FindSingleSharedClass(matches);
        return sharedClass is null ? null : $".{sharedClass}";
    }

}
