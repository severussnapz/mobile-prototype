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

        var prototypeFragments = await PrototypeDomSearchDocumentHelper.LoadPrototypeFragmentsAsync(
            _artefactRepository, _artefactStorageService, request.ProjectId, cancellationToken);
        if (prototypeFragments.Count == 0)
        {
            return new PrototypeDomSearchResult(Matches: [], Truncated: false, TotalMatches: 0);
        }

        var query = request.Query.Trim();
        var matchedCandidates = await PrototypeDomSearchResultBuilder.CollectSelectorMatchesAsync(
            prototypeFragments, query, ExcludedTags, cancellationToken);

        if (matchedCandidates.Count == 0)
        {
            matchedCandidates = await PrototypeDomSearchResultBuilder.CollectClassTokenMatchesAsync(
                prototypeFragments, query, ExcludedTags, cancellationToken);
        }

        if (matchedCandidates.Count == 0)
        {
            matchedCandidates = await PrototypeDomSearchResultBuilder.CollectTextMatchesAsync(
                prototypeFragments, query, ExcludedTags, cancellationToken);
        }

        return PrototypeDomSearchResultBuilder.BuildRankedResult(
            matchedCandidates, request.ProjectId, query, MaxResultCount, _logger);
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

        var prototypeFragments = await PrototypeDomSearchDocumentHelper.LoadPrototypeFragmentsAsync(
            _artefactRepository, _artefactStorageService, request.ProjectId, cancellationToken);
        var resolved = PrototypeDomSearchDocumentHelper.OpenFragmentByScope(
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
                .Select(element => PrototypeDomSearchDocumentHelper.BuildSearchMatch(fragmentPath, element))
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

    public async Task<PrototypeDomSearchResult> ListAllInScopeAsync(
        Guid projectId,
        string scope,
        CancellationToken cancellationToken)
    {
        var prototypeFragments = await PrototypeDomSearchDocumentHelper.LoadPrototypeFragmentsAsync(
            _artefactRepository, _artefactStorageService, projectId, cancellationToken);
        var resolved = PrototypeDomSearchDocumentHelper.OpenFragmentByScope(prototypeFragments, scope);
        if (resolved is null)
        {
            return new PrototypeDomSearchResult(Matches: [], Truncated: false, TotalMatches: 0);
        }

        var (fragmentPath, fragmentHtml) = resolved.Value;
        using var browsingContext = BrowsingContext.New(AngleSharp.Configuration.Default);
        using var document = await browsingContext.OpenAsync(
            response => response.Content(fragmentHtml), cancellationToken);
        var elements = PrototypeDomSearchDocumentHelper.BuildScopeMatches(document, fragmentPath, ExcludedTags, MaxResultCount);

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
        var prototypeFragments = await PrototypeDomSearchDocumentHelper.LoadPrototypeFragmentsAsync(
            _artefactRepository, _artefactStorageService, projectId, cancellationToken);
        var resolved = PrototypeDomSearchDocumentHelper.OpenFragmentByScope(prototypeFragments, scope);
        if (resolved is null)
        {
            return [];
        }

        var (_, fragmentHtml) = resolved.Value;
        using var browsingContext = BrowsingContext.New(AngleSharp.Configuration.Default);
        using var document = await browsingContext.OpenAsync(
            response => response.Content(fragmentHtml), cancellationToken);
        var classNames = PrototypeDomSearchDocumentHelper.CollectClassNames(document, ExcludedTags);
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

        var sharedClass = PrototypeDomSearchDocumentHelper.FindSingleSharedClass(elements.Matches);
        return sharedClass is null ? null : $".{sharedClass}";
    }

    public string? ResolveConfirmedSelectorFromMatches(IReadOnlyList<PrototypeDomSearchMatch> matches)
    {
        ArgumentNullException.ThrowIfNull(matches);
        var sharedClass = PrototypeDomSearchDocumentHelper.FindSingleSharedClass(matches);
        return sharedClass is null ? null : $".{sharedClass}";
    }

}
