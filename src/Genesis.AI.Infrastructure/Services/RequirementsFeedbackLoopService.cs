using System.Globalization;
using System.Text;
using Genesis.AI.Domain.AggregatesModel.PrototypeLockAggregate;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.AggregatesModel.UiDeltaAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

public sealed class RequirementsFeedbackLoopService : IRequirementsFeedbackLoopService
{
    private readonly IUiDeltaRepository _uiDeltaRepository;
    private readonly IPrototypeLockRepository _prototypeLockRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;
    private readonly IRequirementImpactClassifier _requirementImpactClassifier;
    private readonly TimeProvider _timeProvider;

    public RequirementsFeedbackLoopService(
        IUiDeltaRepository uiDeltaRepository,
        IPrototypeLockRepository prototypeLockRepository,
        IProjectRepository projectRepository,
        IArtefactRepository artefactRepository,
        IArtefactStorageService artefactStorageService,
        IRequirementImpactClassifier requirementImpactClassifier,
        TimeProvider timeProvider)
    {
        _uiDeltaRepository = uiDeltaRepository ?? throw new ArgumentNullException(nameof(uiDeltaRepository));
        _prototypeLockRepository = prototypeLockRepository ?? throw new ArgumentNullException(nameof(prototypeLockRepository));
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _artefactStorageService = artefactStorageService ?? throw new ArgumentNullException(nameof(artefactStorageService));
        _requirementImpactClassifier = requirementImpactClassifier ?? throw new ArgumentNullException(nameof(requirementImpactClassifier));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task RecordUiDeltaAsync(UiDeltaRecordRequest request, CancellationToken cancellationToken)
    {
        var impact = await _requirementImpactClassifier.ClassifyAsync(
            request.UserRequest,
            request.BeforeSummary,
            request.AfterSummary,
            cancellationToken);

        var uiDelta = new UiDelta(
            request.ProjectId,
            request.StageId,
            request.RequirementId,
            request.TargetId,
            request.FilePath,
            request.OperationType,
            request.SourceType,
            request.UserRequest,
            request.BeforeSummary,
            request.AfterSummary,
            impact,
            request.CreatedBy,
            _timeProvider,
            request.ConversationId,
            request.MessageId);

        await _uiDeltaRepository.AddAsync(uiDelta, cancellationToken);
        await _uiDeltaRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PrototypeLockResult> LockPrototypeAsync(
        Guid projectId,
        string requirementId,
        string requirementFilePath,
        string lockedBy,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requirementId);
        ArgumentException.ThrowIfNullOrWhiteSpace(requirementFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(lockedBy);

        var prototypeStage = await GetPrototypeStageAsync(projectId, cancellationToken);
        var lockedAt = await MarkPrototypeLockedAsync(projectId, prototypeStage.Id, lockedBy, cancellationToken);

        var substantiveUnlocked = await _uiDeltaRepository.GetUnlockedSubstantiveByRequirementAsync(
            projectId,
            requirementId,
            cancellationToken);

        if (substantiveUnlocked.Count == 0)
        {
            return new PrototypeLockResult(
                true,
                "Prototype locked. No new substantive UI deltas for this requirement.",
                0,
                lockedAt,
                Guid.Empty);
        }

        var lockBatchId = Guid.NewGuid();
        await AppendDeltasToRequirementAsync(
            projectId,
            requirementId,
            requirementFilePath,
            substantiveUnlocked,
            lockBatchId,
            lockedAt,
            lockedBy,
            cancellationToken);

        var ids = substantiveUnlocked.Select(delta => delta.Id).ToList();
        await _uiDeltaRepository.MarkLockedBatchAsync(
            ids,
            lockBatchId,
            requirementFilePath,
            lockedAt,
            cancellationToken);

        return new PrototypeLockResult(
            true,
            $"Prototype locked. Appended {substantiveUnlocked.Count} substantive UI deltas.",
            substantiveUnlocked.Count,
            lockedAt,
            lockBatchId);
    }

    private async Task<PipelineStage> GetPrototypeStageAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project '{projectId}' was not found.");

        return project.PipelineStages.FirstOrDefault(stage => stage.StageType == StageType.Prototype)
            ?? throw new InvalidOperationException("Prototype stage not found for project.");
    }

    private async Task<DateTimeOffset> MarkPrototypeLockedAsync(
        Guid projectId,
        Guid stageId,
        string lockedBy,
        CancellationToken cancellationToken)
    {
        var lockRow = await _prototypeLockRepository.GetByStageIdAsync(stageId, cancellationToken);
        if (lockRow is null)
        {
            lockRow = new PrototypeLock(projectId, stageId, _timeProvider);
            await _prototypeLockRepository.AddAsync(lockRow, cancellationToken);
        }

        var lockedAt = _timeProvider.GetUtcNow();
        lockRow.MarkLocked(lockedAt, lockedBy);
        await _prototypeLockRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        return lockedAt;
    }

    private async Task AppendDeltasToRequirementAsync(
        Guid projectId,
        string requirementId,
        string requirementFilePath,
        IReadOnlyList<UiDelta> substantiveUnlocked,
        Guid lockBatchId,
        DateTimeOffset lockedAt,
        string lockedBy,
        CancellationToken cancellationToken)
    {
        var requirementArtefact = await _artefactRepository.GetByProjectAndFilePathAsync(
            projectId,
            requirementFilePath,
            cancellationToken)
            ?? throw new InvalidOperationException($"Requirement artefact '{requirementFilePath}' was not found.");

        var requirementContent = await _artefactStorageService.GetContentAsync(requirementArtefact.S3Key, cancellationToken)
            ?? throw new InvalidOperationException($"Requirement artefact '{requirementFilePath}' content could not be loaded.");

        var updatedContent = requirementContent + BuildUiDecisionAppendix(requirementId, lockBatchId, lockedAt, substantiveUnlocked);
        var nextVersion = await _artefactRepository.GetNextVersionForFileAsync(projectId, requirementFilePath, cancellationToken);

        var storageKey = await _artefactStorageService.SaveContentAsync(
            projectId,
            requirementFilePath,
            nextVersion,
            updatedContent,
            requirementArtefact.ContentType,
            cancellationToken);

        var updatedArtefact = Domain.AggregatesModel.ArtefactAggregate.Artefact.CreateS3Artefact(
            projectId,
            nextVersion,
            requirementFilePath,
            storageKey,
            requirementArtefact.ContentType,
            Encoding.UTF8.GetByteCount(updatedContent),
            lockedBy,
            _timeProvider,
            true);

        await _artefactRepository.AddAsync(updatedArtefact, cancellationToken);
        await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        await _artefactRepository.DeletePreviousVersionsAsync(projectId, requirementFilePath, nextVersion, cancellationToken);
    }

    private static string BuildUiDecisionAppendix(
        string requirementId,
        Guid lockBatchId,
        DateTimeOffset lockedAt,
        IReadOnlyList<UiDelta> deltas)
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"## UI/UX decisions made during prototyping ({requirementId})");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Locked at: {lockedAt:O}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Lock batch id: {lockBatchId}");
        sb.AppendLine();

        for (var index = 0; index < deltas.Count; index++)
        {
            var delta = deltas[index];
            sb.AppendLine(CultureInfo.InvariantCulture, $"### Decision {index + 1}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"- Target: {delta.TargetId}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"- Operation: {delta.OperationType}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"- Source: {delta.SourceType}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"- User request: {delta.UserRequest ?? "(deterministic structural operation)"}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"- Before: {delta.BeforeSummary}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"- After: {delta.AfterSummary}");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
