using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Domain.Commands.ApproveRequirementChange;

public sealed class ApproveRequirementChangeCommandHandler
{
    private readonly IRequirementChangeRepository _repository;
    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;
    private readonly TimeProvider _timeProvider;

    public ApproveRequirementChangeCommandHandler(
        IRequirementChangeRepository repository,
        IArtefactRepository artefactRepository,
        IArtefactStorageService artefactStorageService,
        TimeProvider timeProvider)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _artefactStorageService = artefactStorageService ?? throw new ArgumentNullException(nameof(artefactStorageService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task Handle(
        ApproveRequirementChangeCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var change = await _repository.GetByIdForProjectAsync(
            command.ChangeId,
            command.ProjectId,
            cancellationToken)
            ?? throw new InvalidOperationException(
                $"Requirement change '{command.ChangeId}' not found.");

        change.Approve(
            approvedAcText: command.ApprovedAcText,
            clinicalSafetyImpact: command.ClinicalSafetyImpact,
            igImpact: command.IgImpact,
            securityImpact: command.SecurityImpact,
            approvedBy: command.ApprovedBy,
            timeProvider: _timeProvider);

        if (change.ChangeType != ChangeType.Contradiction &&
            !string.IsNullOrWhiteSpace(change.ApprovedAcText))
        {
            await InsertAcTextIntoReqFileAsync(change, cancellationToken);
        }

        await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task InsertAcTextIntoReqFileAsync(
        RequirementChange change,
        CancellationToken cancellationToken)
    {
        // Look up the full REQ file path from the artefact manifest.
        // The req_id is e.g. REQ-001 but the actual file path may be
        // requirements/increment-1-inbox-and-triage/REQ-001-unified-inbound-inbox.md
        var allArtefacts = await _artefactRepository.GetProjectArtefactManifestAsync(
            change.ProjectId, cancellationToken);

        var reqPrefix = $"requirements/";
        var reqIdPrefix = $"{change.ReqId}-";
        var reqIdExact = $"{change.ReqId}.md";

        var artefact = allArtefacts.FirstOrDefault(artefact =>
            artefact.FilePath.StartsWith(reqPrefix, StringComparison.OrdinalIgnoreCase) &&
            (artefact.FilePath.Contains($"/{change.ReqId}-", StringComparison.OrdinalIgnoreCase) ||
             artefact.FilePath.EndsWith($"/{change.ReqId}.md", StringComparison.OrdinalIgnoreCase)));

        if (artefact is null)
        {
            return;
        }

        var reqFilePath = artefact.FilePath;

        var content = await _artefactStorageService.GetContentAsync(
            artefact.S3Key, cancellationToken);

        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        var changeId = $"CHANGE-{change.Id.ToString("N")[..8].ToUpperInvariant()}";
        var updatedContent = AcInsertionHelper.InsertAcText(
            content,
            change.ApprovedAcText!,
            changeId,
            change.RaisingPipeline);

        var nextVersion = await _artefactRepository.GetNextVersionForFileAsync(
            change.ProjectId, reqFilePath, cancellationToken);

        var storageKey = await _artefactStorageService.SaveContentAsync(
            change.ProjectId,
            reqFilePath,
            nextVersion,
            updatedContent,
            "text/markdown",
            cancellationToken);

        var newArtefact = Artefact.CreateS3Artefact(
            change.ProjectId,
            nextVersion,
            reqFilePath,
            storageKey,
            "text/markdown",
            System.Text.Encoding.UTF8.GetByteCount(updatedContent),
            change.ApprovedBy ?? "system",
            _timeProvider,
            true);

        await _artefactRepository.AddAsync(newArtefact, cancellationToken);
    }
}
