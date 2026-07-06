using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

public sealed class StructuralEditMutationService : IStructuralEditMutationService
{
    private const string FragmentPrefix = "prototype/fragments/";
    private const string ScreenPrefix = "prototype/fragments/screen-";

    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;
    private readonly IPrototypeAssemblyService _prototypeAssemblyService;
    private readonly StructuralEditDraftService _draftService;

    public StructuralEditMutationService(
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

    public Task<StructuralEditResult> ToggleVisibilityAsync(
        Guid projectId,
        StructuralEditRequest request,
        string createdBy,
        CancellationToken cancellationToken)
    {
        return ToggleVisibilityInternalAsync(projectId, request, createdBy, cancellationToken);
    }

    public Task<StructuralEditResult> DuplicateAsync(
        Guid projectId,
        StructuralEditRequest request,
        string createdBy,
        CancellationToken cancellationToken)
    {
        return DuplicateInternalAsync(projectId, request, createdBy, cancellationToken);
    }

    public Task<StructuralEditResult> DeleteAsync(
        Guid projectId,
        StructuralEditRequest request,
        string createdBy,
        CancellationToken cancellationToken)
    {
        return DeleteInternalAsync(projectId, request, createdBy, cancellationToken);
    }

    private async Task<StructuralEditResult> ToggleVisibilityInternalAsync(
        Guid projectId,
        StructuralEditRequest request,
        string createdBy,
        CancellationToken cancellationToken)
    {
        if (!TryGetFragmentPath(request, FragmentPrefix, "fragmentPath is required and must be under prototype/fragments/.", out var fragmentPath, out var invalidPathResult))
        {
            return invalidPathResult!;
        }

        if (!request.Hidden.HasValue)
        {
            return new StructuralEditResult(false, "hidden is required for toggle_visibility.", []);
        }

        var loadResult = await LoadArtefactWithContentAsync(projectId, fragmentPath!, cancellationToken);
        if (loadResult.Error is not null)
        {
            return loadResult.Error;
        }

        var updatedContent = StructuralEditHtmlUtilities.ToggleHiddenOnRootSection(loadResult.Content!, request.Hidden.Value);
        if (string.Equals(updatedContent, loadResult.Content, StringComparison.Ordinal))
        {
            return new StructuralEditResult(true, "No visibility change required.", [fragmentPath!]);
        }

        var validation = StructuralEditHtmlUtilities.ValidateDraftFragmentContent(updatedContent, fragmentPath!);
        if (!validation.IsValid)
        {
            return new StructuralEditResult(false, validation.Reason ?? "Draft validation failed.", []);
        }

        var draft = await _draftService.SaveDraftVersionAsync(projectId, fragmentPath!, updatedContent, createdBy, cancellationToken);
        if (!await PromoteOrDiscardAsync(draft, cancellationToken))
        {
            return new StructuralEditResult(false, $"Draft promotion failed for {fragmentPath}.", []);
        }

        await _artefactRepository.DeletePreviousVersionsAsync(projectId, fragmentPath!, draft.Version, cancellationToken);
        await _prototypeAssemblyService.AssemblePrototypeAsync(projectId, cancellationToken);

        await _draftService.TryRecordUiDeltaAsync(
            projectId,
            createdBy,
            "toggle_visibility",
            "structural_edit",
            fragmentPath!,
            fragmentPath!,
            StructuralEditDraftService.BuildStructuralUserRequest("toggle_visibility", request),
            request.Hidden.Value ? "Fragment was visible." : "Fragment was hidden.",
            request.Hidden.Value ? "Fragment is now hidden." : "Fragment is now visible.",
            cancellationToken);

        return new StructuralEditResult(true, "Fragment visibility updated.", [fragmentPath!]);
    }

    private async Task<StructuralEditResult> DuplicateInternalAsync(
        Guid projectId,
        StructuralEditRequest request,
        string createdBy,
        CancellationToken cancellationToken)
    {
        if (!TryGetFragmentPath(request, ScreenPrefix, "fragmentPath is required and must be a screen fragment path.", out var fragmentPath, out var invalidPathResult))
        {
            return invalidPathResult!;
        }

        var loadResult = await LoadArtefactWithContentAsync(projectId, fragmentPath!, cancellationToken);
        if (loadResult.Error is not null)
        {
            return loadResult.Error;
        }

        var duplicateTarget = await BuildDuplicateTargetAsync(projectId, fragmentPath!, loadResult.Content!, cancellationToken);
        if (duplicateTarget.Error is not null)
        {
            return duplicateTarget.Error;
        }

        var draft = await _draftService.SaveDraftVersionAsync(projectId, duplicateTarget.Path!, duplicateTarget.Content!, createdBy, cancellationToken);
        if (!await PromoteOrDiscardAsync(draft, cancellationToken))
        {
            return new StructuralEditResult(false, $"Draft promotion failed for {duplicateTarget.Path}.", []);
        }

        await _artefactRepository.DeletePreviousVersionsAsync(projectId, duplicateTarget.Path!, draft.Version, cancellationToken);
        await _prototypeAssemblyService.AssemblePrototypeAsync(projectId, cancellationToken);

        await _draftService.TryRecordUiDeltaAsync(
            projectId,
            createdBy,
            "duplicate",
            "structural_edit",
            duplicateTarget.Path!,
            duplicateTarget.Path!,
            StructuralEditDraftService.BuildStructuralUserRequest("duplicate", request),
            $"Source fragment: {fragmentPath}.",
            $"Created duplicate fragment: {duplicateTarget.Path}.",
            cancellationToken);

        return new StructuralEditResult(true, "Screen fragment duplicated.", [fragmentPath!, duplicateTarget.Path!]);
    }

    private async Task<StructuralEditResult> DeleteInternalAsync(
        Guid projectId,
        StructuralEditRequest request,
        string createdBy,
        CancellationToken cancellationToken)
    {
        if (!TryGetFragmentPath(request, ScreenPrefix, "fragmentPath is required and must be a screen fragment path.", out var fragmentPath, out var invalidPathResult))
        {
            return invalidPathResult!;
        }

        var loadResult = await LoadArtefactWithContentAsync(projectId, fragmentPath!, cancellationToken);
        if (loadResult.Error is not null)
        {
            return loadResult.Error;
        }

        var backupDraft = await _draftService.SaveDraftVersionAsync(projectId, fragmentPath!, loadResult.Content!, createdBy, cancellationToken);

        try
        {
            await _artefactStorageService.DeleteContentAsync(loadResult.Artefact!.S3Key, cancellationToken);
            await _artefactRepository.DeleteByIdAsync(loadResult.Artefact.Id, cancellationToken);
            await _prototypeAssemblyService.AssemblePrototypeAsync(projectId, cancellationToken);
            await _draftService.DiscardDraftAsync(backupDraft, cancellationToken);

            await _draftService.TryRecordUiDeltaAsync(
                projectId,
                createdBy,
                "delete",
                "structural_edit",
                fragmentPath!,
                fragmentPath!,
                StructuralEditDraftService.BuildStructuralUserRequest("delete", request),
                $"Fragment existed: {fragmentPath}.",
                $"Fragment deleted: {fragmentPath}.",
                cancellationToken);

            return new StructuralEditResult(true, "Screen fragment deleted.", [fragmentPath!]);
        }
        catch
        {
            if (await _draftService.PromoteDraftAsync(backupDraft.Id, cancellationToken))
            {
                await _artefactRepository.DeletePreviousVersionsAsync(projectId, fragmentPath!, backupDraft.Version, cancellationToken);
            }

            throw;
        }
    }

    private async Task<(Artefact? Artefact, string? Content, StructuralEditResult? Error)> LoadArtefactWithContentAsync(
        Guid projectId,
        string fragmentPath,
        CancellationToken cancellationToken)
    {
        var artefact = await _artefactRepository.GetByProjectAndFilePathAsync(projectId, fragmentPath, cancellationToken);
        if (artefact is null)
        {
            return (null, null, new StructuralEditResult(false, $"Fragment not found: {fragmentPath}", []));
        }

        var content = await _artefactStorageService.GetContentAsync(artefact.S3Key, cancellationToken);
        if (content is null)
        {
            return (null, null, new StructuralEditResult(false, $"Unable to load content for {fragmentPath}", []));
        }

        return (artefact, content, null);
    }

    private async Task<(string? Path, string? Content, StructuralEditResult? Error)> BuildDuplicateTargetAsync(
        Guid projectId,
        string fragmentPath,
        string sourceContent,
        CancellationToken cancellationToken)
    {
        var publishedArtefacts = await _artefactRepository.GetByProjectIdAsync(projectId, cancellationToken);
        var nextScreenNumber = publishedArtefacts
            .Where(item => item.FilePath.StartsWith(ScreenPrefix, StringComparison.OrdinalIgnoreCase))
            .Select(item => StructuralEditHtmlUtilities.ExtractScreenNumber(item.FilePath))
            .DefaultIfEmpty(0)
            .Max() + 1;

        var duplicatedPath = StructuralEditHtmlUtilities.BuildScreenPath(nextScreenNumber, $"{StructuralEditHtmlUtilities.ExtractSlug(fragmentPath)}-copy");
        var duplicatedContent = StructuralEditHtmlUtilities.EnsureRootSectionId(sourceContent, Path.GetFileNameWithoutExtension(duplicatedPath));
        var validation = StructuralEditHtmlUtilities.ValidateDraftFragmentContent(duplicatedContent, duplicatedPath);
        if (!validation.IsValid)
        {
            return (null, null, new StructuralEditResult(false, validation.Reason ?? "Draft validation failed.", []));
        }

        return (duplicatedPath, duplicatedContent, null);
    }

    private async Task<bool> PromoteOrDiscardAsync(Artefact draft, CancellationToken cancellationToken)
    {
        if (await _draftService.PromoteDraftAsync(draft.Id, cancellationToken))
        {
            return true;
        }

        await _draftService.DiscardDraftAsync(draft, cancellationToken);
        return false;
    }

    private static bool TryGetFragmentPath(
        StructuralEditRequest request,
        string requiredPrefix,
        string errorMessage,
        out string? fragmentPath,
        out StructuralEditResult? error)
    {
        fragmentPath = request.FragmentPath?.Trim();
        if (string.IsNullOrWhiteSpace(fragmentPath) || !fragmentPath.StartsWith(requiredPrefix, StringComparison.OrdinalIgnoreCase))
        {
            error = new StructuralEditResult(false, errorMessage, []);
            return false;
        }

        error = null;
        return true;
    }
}
