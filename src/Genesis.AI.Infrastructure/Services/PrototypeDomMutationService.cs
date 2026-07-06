using System.Text;
using AngleSharp;
using AngleSharp.Dom;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Genesis.AI.Infrastructure.Services;

public sealed class PrototypeDomMutationService : IPrototypeDomMutationService
{
    private readonly ILogger<PrototypeDomMutationService> _logger;
    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;
    private readonly IPrototypeAssemblyService _prototypeAssemblyService;
    private readonly TimeProvider _timeProvider;

    public PrototypeDomMutationService(
        ILogger<PrototypeDomMutationService> logger,
        IArtefactRepository artefactRepository,
        IArtefactStorageService artefactStorageService,
        IPrototypeAssemblyService prototypeAssemblyService,
        TimeProvider timeProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _artefactStorageService = artefactStorageService ?? throw new ArgumentNullException(nameof(artefactStorageService));
        _prototypeAssemblyService = prototypeAssemblyService ?? throw new ArgumentNullException(nameof(prototypeAssemblyService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<PrototypeDomMutationResult> ApplyMutationAsync(
        PrototypeDomMutationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var batchResult = await ApplyBatchMutationAsync([request], cancellationToken);
        var first = batchResult.Results[0];
        return new PrototypeDomMutationResult(
            Success: first.Success,
            Message: first.Message,
            FragmentPath: first.FragmentPath,
            Version: first.Version);
    }

    public async Task<PrototypeDomBatchMutationResult> ApplyBatchMutationAsync(
        IReadOnlyList<PrototypeDomMutationRequest> requests,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requests);
        if (requests.Count == 0)
        {
            return new PrototypeDomBatchMutationResult(
                TotalMutations: 0, SuccessfulMutations: 0, Results: [], PersistedFragments: []);
        }

        var indexedRequests = requests
            .Select((request, index) => (Request: request, Index: index))
            .ToList();

        var orderedResults = new PrototypeDomBatchMutationItemResult[requests.Count];
        var persistedFragments = new List<PrototypeDomMutationFragmentResult>();
        var shouldAssembleByProject = new HashSet<Guid>();

        foreach (var grouped in indexedRequests.GroupBy(
                     item => (item.Request.ProjectId, item.Request.FragmentPath),
                     item => item,
                     EqualityComparer<(Guid ProjectId, string FragmentPath)>.Default))
        {
            await ProcessFragmentGroupAsync(
                grouped, requests, orderedResults, persistedFragments,
                shouldAssembleByProject, cancellationToken);
        }

        foreach (var projectId in shouldAssembleByProject)
        {
            await _prototypeAssemblyService.AssemblePrototypeAsync(projectId, cancellationToken);
        }

        var successfulMutations = orderedResults.Count(result => result.Success);
        return new PrototypeDomBatchMutationResult(
            TotalMutations: requests.Count,
            SuccessfulMutations: successfulMutations,
            Results: orderedResults,
            PersistedFragments: persistedFragments);
    }

    private async Task ProcessFragmentGroupAsync(
        IEnumerable<(PrototypeDomMutationRequest Request, int Index)> grouped,
        IReadOnlyList<PrototypeDomMutationRequest> requests,
        PrototypeDomBatchMutationItemResult[] orderedResults,
        List<PrototypeDomMutationFragmentResult> persistedFragments,
        HashSet<Guid> shouldAssembleByProject,
        CancellationToken cancellationToken)
    {
        var items = grouped.ToList();
        var projectId = items[0].Request.ProjectId;
        var fragmentPath = items[0].Request.FragmentPath;
        var firstRequest = items[0].Request;

        if (string.IsNullOrWhiteSpace(fragmentPath))
        {
            SetGroupFailureResults(items, orderedResults, "fragment_path is required", null);
            return;
        }

        var (originalHtml, contentType, loadError) = await LoadFragmentAsync(
            projectId, fragmentPath, cancellationToken);
        if (loadError is not null)
        {
            SetGroupFailureResults(items, orderedResults, loadError, fragmentPath);
            return;
        }

        await ApplyAndPersistMutationsAsync(
            items, requests, orderedResults, persistedFragments, shouldAssembleByProject,
            originalHtml!, contentType!, projectId, fragmentPath, firstRequest.CreatedBy,
            cancellationToken);
    }

    private async Task ApplyAndPersistMutationsAsync(
        List<(PrototypeDomMutationRequest Request, int Index)> items,
        IReadOnlyList<PrototypeDomMutationRequest> requests,
        PrototypeDomBatchMutationItemResult[] orderedResults,
        List<PrototypeDomMutationFragmentResult> persistedFragments,
        HashSet<Guid> shouldAssembleByProject,
        string originalHtml,
        string contentType,
        Guid projectId,
        string fragmentPath,
        string createdBy,
        CancellationToken cancellationToken)
    {
        var browsingContext = BrowsingContext.New(AngleSharp.Configuration.Default);
        var document = await browsingContext.OpenAsync(
            response => response.Content(originalHtml), cancellationToken);

        var successfulIndexes = ApplyMutationsToDocument(items, orderedResults, document, fragmentPath);
        if (successfulIndexes.Count == 0)
        {
            return;
        }

        var serialisedHtml = PrototypeDomMutationHelper.SerializeDocument(document, originalHtml);
        if (string.Equals(serialisedHtml, originalHtml, StringComparison.Ordinal))
        {
            SetGroupNoOpResults(successfulIndexes, requests, orderedResults, fragmentPath);
            return;
        }

        var version = await PersistMutatedFragmentAsync(
            projectId, fragmentPath, serialisedHtml, contentType, createdBy, cancellationToken);

        persistedFragments.Add(new PrototypeDomMutationFragmentResult(projectId, fragmentPath, version));
        shouldAssembleByProject.Add(projectId);
        SetGroupSuccessResults(successfulIndexes, requests, orderedResults, fragmentPath, version);
    }

    private static void SetGroupSuccessResults(
        List<int> successfulIndexes,
        IReadOnlyList<PrototypeDomMutationRequest> requests,
        PrototypeDomBatchMutationItemResult[] orderedResults,
        string fragmentPath,
        int version)
    {
        foreach (var successIndex in successfulIndexes)
        {
            orderedResults[successIndex] = new PrototypeDomBatchMutationItemResult(
                NodeKey: requests[successIndex].NodeKey,
                Success: true,
                Message: "ok",
                FragmentPath: fragmentPath,
                Version: version);
        }
    }

    private async Task<(string? Html, string? ContentType, string? Error)> LoadFragmentAsync(
        Guid projectId, string fragmentPath, CancellationToken cancellationToken)
    {
        var fragmentArtefact = await _artefactRepository.GetByProjectAndFilePathAsync(
            projectId, fragmentPath, cancellationToken);
        if (fragmentArtefact is null)
        {
            return (null, null, "fragment not found");
        }

        var originalHtml = await _artefactStorageService.GetContentAsync(
            fragmentArtefact.S3Key, cancellationToken);
        if (string.IsNullOrWhiteSpace(originalHtml))
        {
            return (null, null, "fragment content missing");
        }

        var contentType = string.IsNullOrWhiteSpace(fragmentArtefact.ContentType)
            ? "text/html"
            : fragmentArtefact.ContentType;

        return (originalHtml, contentType, null);
    }

    private static List<int> ApplyMutationsToDocument(
        List<(PrototypeDomMutationRequest Request, int Index)> items,
        PrototypeDomBatchMutationItemResult[] orderedResults,
        IDocument document,
        string fragmentPath)
    {
        var successfulIndexes = new List<int>();
        foreach (var item in items)
        {
            var targetElement = PrototypeDomMutationHelper.ResolveTargetElement(document, item.Request.NodeKey, fragmentPath);
            if (targetElement is null)
            {
                orderedResults[item.Index] = new PrototypeDomBatchMutationItemResult(
                    NodeKey: item.Request.NodeKey, Success: false,
                    Message: "target element not found", FragmentPath: fragmentPath, Version: null);
                continue;
            }

            var applyError = PrototypeDomMutationHelper.ApplyMutation(
                targetElement, item.Request.Operation, item.Request.Attribute, item.Request.Value);
            if (applyError is not null)
            {
                orderedResults[item.Index] = new PrototypeDomBatchMutationItemResult(
                    NodeKey: item.Request.NodeKey, Success: false,
                    Message: applyError, FragmentPath: fragmentPath, Version: null);
                continue;
            }

            successfulIndexes.Add(item.Index);
        }

        return successfulIndexes;
    }

    private async Task<int> PersistMutatedFragmentAsync(
        Guid projectId, string fragmentPath, string serialisedHtml,
        string contentType, string createdBy, CancellationToken cancellationToken)
    {
        var nextVersion = await _artefactRepository.GetNextVersionForFileAsync(
            projectId, fragmentPath, cancellationToken);
        var storageKey = await _artefactStorageService.SaveContentAsync(
            projectId, fragmentPath, nextVersion, serialisedHtml, contentType, cancellationToken);
        var updatedArtefact = Artefact.CreateS3Artefact(
            projectId, nextVersion, fragmentPath, storageKey, contentType,
            Encoding.UTF8.GetByteCount(serialisedHtml), createdBy, _timeProvider, true);
        await _artefactRepository.AddAsync(updatedArtefact, cancellationToken);
        await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        await _artefactRepository.DeletePreviousVersionsAsync(
            projectId, fragmentPath, nextVersion, cancellationToken);
        return nextVersion;
    }

    private static void SetGroupFailureResults(
        List<(PrototypeDomMutationRequest Request, int Index)> items,
        PrototypeDomBatchMutationItemResult[] orderedResults,
        string message, string? fragmentPath)
    {
        foreach (var item in items)
        {
            orderedResults[item.Index] = new PrototypeDomBatchMutationItemResult(
                NodeKey: item.Request.NodeKey, Success: false,
                Message: message, FragmentPath: fragmentPath, Version: null);
        }
    }

    private static void SetGroupNoOpResults(
        List<int> successfulIndexes,
        IReadOnlyList<PrototypeDomMutationRequest> requests,
        PrototypeDomBatchMutationItemResult[] orderedResults,
        string fragmentPath)
    {
        foreach (var successIndex in successfulIndexes)
        {
            orderedResults[successIndex] = new PrototypeDomBatchMutationItemResult(
                NodeKey: requests[successIndex].NodeKey, Success: true,
                Message: "no-op", FragmentPath: fragmentPath, Version: null);
        }
    }

}
