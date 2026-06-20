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
                TotalMutations: 0,
                SuccessfulMutations: 0,
                Results: [],
                PersistedFragments: []);
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
            var projectId = grouped.Key.ProjectId;
            var fragmentPath = grouped.Key.FragmentPath;
            var firstRequest = grouped.First().Request;

            if (string.IsNullOrWhiteSpace(fragmentPath))
            {
                foreach (var item in grouped)
                {
                    orderedResults[item.Index] = new PrototypeDomBatchMutationItemResult(
                        NodeKey: item.Request.NodeKey,
                        Success: false,
                        Message: "fragment_path is required",
                        FragmentPath: null,
                        Version: null);
                }

                continue;
            }

            var fragmentArtefact = await _artefactRepository.GetByProjectAndFilePathAsync(
                projectId,
                fragmentPath,
                cancellationToken);
            if (fragmentArtefact is null)
            {
                foreach (var item in grouped)
                {
                    orderedResults[item.Index] = new PrototypeDomBatchMutationItemResult(
                        NodeKey: item.Request.NodeKey,
                        Success: false,
                        Message: "fragment not found",
                        FragmentPath: fragmentPath,
                        Version: null);
                }

                continue;
            }

            var originalHtml = await _artefactStorageService.GetContentAsync(fragmentArtefact.S3Key, cancellationToken);
            if (string.IsNullOrWhiteSpace(originalHtml))
            {
                foreach (var item in grouped)
                {
                    orderedResults[item.Index] = new PrototypeDomBatchMutationItemResult(
                        NodeKey: item.Request.NodeKey,
                        Success: false,
                        Message: "fragment content missing",
                        FragmentPath: fragmentPath,
                        Version: null);
                }

                continue;
            }

            var browsingContext = BrowsingContext.New(AngleSharp.Configuration.Default);
            var document = await browsingContext.OpenAsync(
                response => response.Content(originalHtml),
                cancellationToken);

            var successfulIndexes = new List<int>();
            foreach (var item in grouped)
            {
                var targetElement = ResolveTargetElement(document, item.Request.NodeKey, fragmentPath);
                if (targetElement is null)
                {
                    orderedResults[item.Index] = new PrototypeDomBatchMutationItemResult(
                        NodeKey: item.Request.NodeKey,
                        Success: false,
                        Message: "target element not found",
                        FragmentPath: fragmentPath,
                        Version: null);
                    continue;
                }

                var applyError = ApplyMutation(targetElement, item.Request.Operation, item.Request.Attribute, item.Request.Value);
                if (applyError is not null)
                {
                    orderedResults[item.Index] = new PrototypeDomBatchMutationItemResult(
                        NodeKey: item.Request.NodeKey,
                        Success: false,
                        Message: applyError,
                        FragmentPath: fragmentPath,
                        Version: null);
                    continue;
                }

                successfulIndexes.Add(item.Index);
            }

            if (successfulIndexes.Count == 0)
            {
                continue;
            }

            var serialisedHtml = SerializeDocument(document, originalHtml);
            if (string.Equals(serialisedHtml, originalHtml, StringComparison.Ordinal))
            {
                foreach (var successIndex in successfulIndexes)
                {
                    var request = requests[successIndex];
                    orderedResults[successIndex] = new PrototypeDomBatchMutationItemResult(
                        NodeKey: request.NodeKey,
                        Success: true,
                        Message: "no-op",
                        FragmentPath: fragmentPath,
                        Version: null);
                }

                continue;
            }

            var nextVersion = await _artefactRepository.GetNextVersionForFileAsync(
                projectId,
                fragmentPath,
                cancellationToken);

            var contentType = string.IsNullOrWhiteSpace(fragmentArtefact.ContentType)
                ? "text/html"
                : fragmentArtefact.ContentType;
            var storageKey = await _artefactStorageService.SaveContentAsync(
                projectId,
                fragmentPath,
                nextVersion,
                serialisedHtml,
                contentType,
                cancellationToken);

            var updatedArtefact = Artefact.CreateS3Artefact(
                projectId,
                nextVersion,
                fragmentPath,
                storageKey,
                contentType,
                Encoding.UTF8.GetByteCount(serialisedHtml),
                firstRequest.CreatedBy,
                _timeProvider,
                true);

            await _artefactRepository.AddAsync(updatedArtefact, cancellationToken);
            await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            await _artefactRepository.DeletePreviousVersionsAsync(
                projectId,
                fragmentPath,
                nextVersion,
                cancellationToken);

            persistedFragments.Add(new PrototypeDomMutationFragmentResult(projectId, fragmentPath, nextVersion));
            shouldAssembleByProject.Add(projectId);

            foreach (var successIndex in successfulIndexes)
            {
                var request = requests[successIndex];
                orderedResults[successIndex] = new PrototypeDomBatchMutationItemResult(
                    NodeKey: request.NodeKey,
                    Success: true,
                    Message: "ok",
                    FragmentPath: fragmentPath,
                    Version: nextVersion);
            }
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

    private static IElement? ResolveTargetElement(IDocument document, string nodeKey, string fragmentPath)
    {
        var stableLocator = ExtractStableLocator(nodeKey, fragmentPath);
        if (string.IsNullOrWhiteSpace(stableLocator))
        {
            return null;
        }

        if (stableLocator.StartsWith("css:", StringComparison.Ordinal))
        {
            stableLocator = stableLocator[4..];
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
        catch (Exception)
        {
            return null;
        }
    }

    private static string? ApplyMutation(
        IElement element,
        PrototypeDomMutationOperation operation,
        string? attribute,
        string? value)
    {
        switch (operation)
        {
            case PrototypeDomMutationOperation.SetAttribute:
                if (string.IsNullOrWhiteSpace(attribute))
                {
                    return "attribute is required for SetAttribute";
                }

                element.SetAttribute(attribute, value ?? string.Empty);
                return null;

            case PrototypeDomMutationOperation.SetText:
                element.TextContent = value ?? string.Empty;
                return null;

            case PrototypeDomMutationOperation.AddClass:
                if (string.IsNullOrWhiteSpace(value))
                {
                    return "value is required for AddClass";
                }

                element.ClassList.Add(value);
                return null;

            case PrototypeDomMutationOperation.RemoveClass:
                if (string.IsNullOrWhiteSpace(value))
                {
                    return "value is required for RemoveClass";
                }

                element.ClassList.Remove(value);
                return null;

            case PrototypeDomMutationOperation.InsertAdjacentHtml:
                if (string.IsNullOrWhiteSpace(attribute))
                {
                    return "position is required for InsertAdjacentHtml";
                }

                if (!TryParseInsertPosition(attribute, out var insertPosition))
                {
                    return "position must be one of: beforebegin, afterbegin, beforeend, afterend";
                }

                if (string.IsNullOrWhiteSpace(value))
                {
                    return "value is required for InsertAdjacentHtml";
                }

                element.Insert(insertPosition, value);
                return null;

            case PrototypeDomMutationOperation.RemoveElement:
                element.Remove();
                return null;

            default:
                return "unsupported operation";
        }
    }

    private static bool TryParseInsertPosition(string position, out AdjacentPosition adjacentPosition)
    {
        if (position.Equals("beforebegin", StringComparison.OrdinalIgnoreCase))
        {
            adjacentPosition = AdjacentPosition.BeforeBegin;
            return true;
        }

        if (position.Equals("afterbegin", StringComparison.OrdinalIgnoreCase))
        {
            adjacentPosition = AdjacentPosition.AfterBegin;
            return true;
        }

        if (position.Equals("beforeend", StringComparison.OrdinalIgnoreCase))
        {
            adjacentPosition = AdjacentPosition.BeforeEnd;
            return true;
        }

        if (position.Equals("afterend", StringComparison.OrdinalIgnoreCase))
        {
            adjacentPosition = AdjacentPosition.AfterEnd;
            return true;
        }

        adjacentPosition = AdjacentPosition.BeforeEnd;
        return false;
    }

    private static string ExtractStableLocator(string nodeKey, string fragmentPath)
    {
        if (string.IsNullOrWhiteSpace(nodeKey))
        {
            return string.Empty;
        }

        var separatorIndex = nodeKey.IndexOf('|', StringComparison.Ordinal);
        if (separatorIndex < 0)
        {
            return nodeKey.Trim();
        }

        var nodeKeyFragmentPath = nodeKey[..separatorIndex];
        if (!nodeKeyFragmentPath.Equals(fragmentPath, StringComparison.OrdinalIgnoreCase))
        {
            return nodeKey[(separatorIndex + 1)..].Trim();
        }

        return nodeKey[(separatorIndex + 1)..].Trim();
    }

    private static string SerializeDocument(IDocument document, string originalHtml)
    {
        if (LooksLikeDocument(originalHtml))
        {
            var documentElement = document.DocumentElement?.OuterHtml ?? string.Empty;
            var doctype = document.Doctype?.ToHtml();
            if (string.IsNullOrWhiteSpace(doctype))
            {
                return documentElement;
            }

            return string.Concat(doctype, documentElement);
        }

        return document.Body?.InnerHtml ?? string.Empty;
    }

    private static bool LooksLikeDocument(string html)
    {
        var trimmed = html.TrimStart();
        return trimmed.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase);
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
}
