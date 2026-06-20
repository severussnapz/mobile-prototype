using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

public sealed class StructuralEditReorderService : IStructuralEditReorderService
{
    private const string ScreenPrefix = "prototype/fragments/screen-";

    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;
    private readonly IPrototypeAssemblyService _prototypeAssemblyService;
    private readonly StructuralEditDraftService _draftService;

    public StructuralEditReorderService(
        IArtefactRepository artefactRepository,
        IArtefactStorageService artefactStorageService,
        IPrototypeAssemblyService prototypeAssemblyService,
        StructuralEditDraftService draftService)
    {
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _artefactStorageService = artefactStorageService ?? throw new ArgumentNullException(nameof(artefactStorageService));
        _prototypeAssemblyService = prototypeAssemblyService ?? throw new ArgumentNullException(nameof(prototypeAssemblyService));
        _draftService = draftService ?? throw new ArgumentNullException(nameof(draftService));
    }

    public async Task<StructuralEditResult> ApplyAsync(
        Guid projectId,
        StructuralEditRequest request,
        string createdBy,
        CancellationToken cancellationToken)
    {
        if (request.OrderedFragmentPaths is null || request.OrderedFragmentPaths.Count == 0)
        {
            return new StructuralEditResult(false, "orderedFragmentPaths is required for reorder.", []);
        }

        var screenArtefacts = await GetScreenArtefactsAsync(projectId, cancellationToken);
        if (!HasMatchingReorderPaths(screenArtefacts, request.OrderedFragmentPaths))
        {
            return new StructuralEditResult(false, "orderedFragmentPaths must contain exactly the current screen fragment paths.", []);
        }

        var contentByPath = await LoadScreenContentAsync(screenArtefacts, cancellationToken);
        if (contentByPath is null)
        {
            return new StructuralEditResult(false, "Unable to load one or more fragment contents.", []);
        }

        var draftCreation = await CreateReorderDraftsAsync(projectId, request, createdBy, contentByPath, cancellationToken);
        if (draftCreation.Error is not null)
        {
            return draftCreation.Error;
        }

        return await PromoteAndFinalizeAsync(
            projectId,
            request,
            createdBy,
            screenArtefacts,
            draftCreation.DraftWrites,
            draftCreation.AffectedPaths,
            cancellationToken);
    }

    private async Task<List<Artefact>> GetScreenArtefactsAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var publishedArtefacts = await _artefactRepository.GetByProjectIdAsync(projectId, cancellationToken);
        return publishedArtefacts
            .Where(artefact => artefact.FilePath.StartsWith(ScreenPrefix, StringComparison.OrdinalIgnoreCase))
            .OrderBy(artefact => artefact.FilePath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool HasMatchingReorderPaths(IReadOnlyList<Artefact> screenArtefacts, IReadOnlyList<string> requestedPaths)
    {
        var existingPaths = screenArtefacts
            .Select(artefact => artefact.FilePath)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var normalisedRequested = requestedPaths
            .Select(path => path.Trim())
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return existingPaths.SequenceEqual(normalisedRequested, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<Dictionary<string, string>?> LoadScreenContentAsync(
        IReadOnlyList<Artefact> screenArtefacts,
        CancellationToken cancellationToken)
    {
        var contentByPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var artefact in screenArtefacts)
        {
            var content = await _artefactStorageService.GetContentAsync(artefact.S3Key, cancellationToken);
            if (content is null)
            {
                return null;
            }

            contentByPath[artefact.FilePath] = content;
        }

        return contentByPath;
    }

    private async Task<(List<StructuralEditDraftService.DraftWrite> DraftWrites, List<string> AffectedPaths, StructuralEditResult? Error)> CreateReorderDraftsAsync(
        Guid projectId,
        StructuralEditRequest request,
        string createdBy,
        Dictionary<string, string> contentByPath,
        CancellationToken cancellationToken)
    {
        var affectedPaths = new List<string>(request.OrderedFragmentPaths!.Count);
        var draftWrites = new List<StructuralEditDraftService.DraftWrite>(request.OrderedFragmentPaths.Count);

        for (var index = 0; index < request.OrderedFragmentPaths.Count; index++)
        {
            var originalPath = request.OrderedFragmentPaths[index].Trim();
            var targetPath = StructuralEditHtmlUtilities.BuildScreenPath(index + 1, StructuralEditHtmlUtilities.ExtractSlug(originalPath));
            var targetContent = contentByPath[originalPath];

            var validation = StructuralEditHtmlUtilities.ValidateDraftFragmentContent(targetContent, targetPath);
            if (!validation.IsValid)
            {
                await _draftService.DiscardDraftsAsync(draftWrites, cancellationToken);
                return (draftWrites, affectedPaths, new StructuralEditResult(false, validation.Reason ?? "Draft validation failed.", []));
            }

            var draft = await _draftService.SaveDraftVersionAsync(projectId, targetPath, targetContent, createdBy, cancellationToken);
            draftWrites.Add(new StructuralEditDraftService.DraftWrite(draft, targetContent));
            affectedPaths.Add(targetPath);
        }

        return (draftWrites, affectedPaths, null);
    }

    private async Task<StructuralEditResult> PromoteAndFinalizeAsync(
        Guid projectId,
        StructuralEditRequest request,
        string createdBy,
        IReadOnlyList<Artefact> screenArtefacts,
        IReadOnlyList<StructuralEditDraftService.DraftWrite> draftWrites,
        IReadOnlyList<string> affectedPaths,
        CancellationToken cancellationToken)
    {
        try
        {
            var promoted = await PromoteDraftsAsync(draftWrites, cancellationToken);
            if (!promoted)
            {
                return new StructuralEditResult(false, "Draft promotion failed.", []);
            }

            await CleanupVersionsAsync(projectId, draftWrites, cancellationToken);
            await RemovePriorPublishedScreensAsync(screenArtefacts, cancellationToken);
            await _prototypeAssemblyService.AssemblePrototypeAsync(projectId, cancellationToken);

            await _draftService.TryRecordUiDeltaAsync(
                projectId,
                createdBy,
                "reorder",
                "structural_edit",
                string.Join(" -> ", request.OrderedFragmentPaths!),
                "prototype/fragments",
                StructuralEditDraftService.BuildStructuralUserRequest("reorder", request),
                "Reordered screen fragments in prototype flow.",
                $"Applied requested order: {string.Join(", ", request.OrderedFragmentPaths!)}.",
                cancellationToken);

            return new StructuralEditResult(true, "Screen fragments reordered successfully.", affectedPaths.ToList());
        }
        catch
        {
            await _draftService.DiscardDraftsAsync(draftWrites, cancellationToken);
            throw;
        }
    }

    private async Task<bool> PromoteDraftsAsync(
        IReadOnlyList<StructuralEditDraftService.DraftWrite> draftWrites,
        CancellationToken cancellationToken)
    {
        foreach (var draftWrite in draftWrites)
        {
            if (!await _draftService.PromoteDraftAsync(draftWrite.DraftArtefact.Id, cancellationToken))
            {
                await _draftService.DiscardDraftsAsync(draftWrites, cancellationToken);
                return false;
            }
        }

        return true;
    }

    private async Task CleanupVersionsAsync(
        Guid projectId,
        IReadOnlyList<StructuralEditDraftService.DraftWrite> draftWrites,
        CancellationToken cancellationToken)
    {
        foreach (var draftWrite in draftWrites)
        {
            await _artefactRepository.DeletePreviousVersionsAsync(
                projectId,
                draftWrite.DraftArtefact.FilePath,
                draftWrite.DraftArtefact.Version,
                cancellationToken);
        }
    }

    private async Task RemovePriorPublishedScreensAsync(
        IReadOnlyList<Artefact> screenArtefacts,
        CancellationToken cancellationToken)
    {
        foreach (var artefact in screenArtefacts)
        {
            await _artefactStorageService.DeleteContentAsync(artefact.S3Key, cancellationToken);
            await _artefactRepository.DeleteByIdAsync(artefact.Id, cancellationToken);
        }
    }
}
