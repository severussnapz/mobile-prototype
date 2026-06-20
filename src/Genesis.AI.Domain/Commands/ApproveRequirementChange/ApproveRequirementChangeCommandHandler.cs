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

        var change = await _repository.GetByIdAsync(command.ChangeId, cancellationToken)
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
        var reqFilePath = $"requirements/{change.ReqId}.md";

        var artefact = await _artefactRepository.GetByProjectAndFilePathAsync(
            change.ProjectId, reqFilePath, cancellationToken);

        if (artefact is null)
        {
            return;
        }

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

        await _artefactStorageService.SaveContentAsync(
            change.ProjectId,
            reqFilePath,
            nextVersion,
            updatedContent,
            "text/markdown",
            cancellationToken);
    }
}
