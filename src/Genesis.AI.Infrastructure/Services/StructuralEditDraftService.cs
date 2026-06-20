using System.Text;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

public sealed class StructuralEditDraftService
{
    internal sealed record DraftWrite(Artefact DraftArtefact, string Content);

    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;
    private readonly IProjectRepository _projectRepository;
    private readonly IRequirementsFeedbackLoopService? _requirementsFeedbackLoopService;
    private readonly TimeProvider _timeProvider;

    public StructuralEditDraftService(
        IArtefactRepository artefactRepository,
        IArtefactStorageService artefactStorageService,
        IProjectRepository projectRepository,
        IRequirementsFeedbackLoopService? requirementsFeedbackLoopService,
        TimeProvider timeProvider)
    {
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _artefactStorageService = artefactStorageService ?? throw new ArgumentNullException(nameof(artefactStorageService));
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _requirementsFeedbackLoopService = requirementsFeedbackLoopService;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    internal async Task<Artefact> SaveDraftVersionAsync(
        Guid projectId,
        string filePath,
        string content,
        string createdBy,
        CancellationToken cancellationToken)
    {
        var nextVersion = await _artefactRepository.GetNextVersionForFileAsync(projectId, filePath, cancellationToken);
        var contentType = StructuralEditHtmlUtilities.ResolveContentType(filePath);

        var storageKey = await _artefactStorageService.SaveContentAsync(
            projectId,
            filePath,
            nextVersion,
            content,
            contentType,
            cancellationToken);

        var artefact = Artefact.CreateS3Artefact(
            projectId,
            nextVersion,
            filePath,
            storageKey,
            contentType,
            Encoding.UTF8.GetByteCount(content),
            createdBy,
            _timeProvider,
            false);

        await _artefactRepository.AddAsync(artefact, cancellationToken);
        await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        return artefact;
    }

    internal async Task<bool> PromoteDraftAsync(Guid draftId, CancellationToken cancellationToken)
    {
        var trackedDraft = await _artefactRepository.GetByIdAsync(draftId, cancellationToken);
        if (trackedDraft is null)
        {
            return false;
        }

        trackedDraft.PromoteToPublished();
        await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    internal async Task DiscardDraftAsync(Artefact draftArtefact, CancellationToken cancellationToken)
    {
        await _artefactStorageService.DeleteContentAsync(draftArtefact.S3Key, cancellationToken);
        await _artefactRepository.DeleteByIdAsync(draftArtefact.Id, cancellationToken);
    }

    internal async Task DiscardDraftsAsync(IEnumerable<DraftWrite> draftWrites, CancellationToken cancellationToken)
    {
        foreach (var draftWrite in draftWrites)
        {
            await DiscardDraftAsync(draftWrite.DraftArtefact, cancellationToken);
        }
    }

    internal async Task TryRecordUiDeltaAsync(
        Guid projectId,
        string createdBy,
        string operationType,
        string sourceType,
        string targetId,
        string filePath,
        string? userRequest,
        string beforeSummary,
        string afterSummary,
        CancellationToken cancellationToken)
    {
        if (_requirementsFeedbackLoopService is null)
        {
            return;
        }

        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project is null)
        {
            return;
        }

        var prototypeStage = project.PipelineStages.FirstOrDefault(stage => stage.StageType == StageType.Prototype);
        if (prototypeStage is null)
        {
            return;
        }

        await _requirementsFeedbackLoopService.RecordUiDeltaAsync(
            new UiDeltaRecordRequest(
                projectId,
                prototypeStage.Id,
                RequirementId: null,
                targetId,
                filePath,
                operationType,
                sourceType,
                userRequest,
                beforeSummary,
                afterSummary,
                createdBy),
            cancellationToken);
    }

    internal static string BuildStructuralUserRequest(string operation, StructuralEditRequest request)
    {
        return operation switch
        {
            "reorder" => $"Reordered screens to: {string.Join(", ", request.OrderedFragmentPaths ?? [])}",
            "toggle_visibility" => $"Set {(request.Hidden == true ? "hidden" : "visible")} state for {request.FragmentPath}",
            "duplicate" => $"Duplicated fragment {request.FragmentPath}",
            "delete" => $"Deleted fragment {request.FragmentPath}",
            _ => "Applied structural edit"
        };
    }
}
