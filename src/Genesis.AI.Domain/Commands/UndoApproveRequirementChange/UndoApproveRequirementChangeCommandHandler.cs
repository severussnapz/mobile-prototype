using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Domain.Commands.UndoApproveRequirementChange;

public sealed class UndoApproveRequirementChangeCommandHandler
{
    private readonly IRequirementChangeRepository _repository;
    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;
    private readonly TimeProvider _timeProvider;

    public UndoApproveRequirementChangeCommandHandler(
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
        UndoApproveRequirementChangeCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var change = await _repository.GetByIdAsync(command.ChangeId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Requirement change '{command.ChangeId}' not found.");

        change.Undo(
            undoneBy: command.UndoneBy,
            rationale: command.UndoRationale,
            timeProvider: _timeProvider);

        await RestorePreviousReqVersionAsync(change, cancellationToken);

        await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task RestorePreviousReqVersionAsync(
        RequirementChange change,
        CancellationToken cancellationToken)
    {
        var reqFilePath = $"requirements/{change.ReqId}.md";

        var previousArtefact = await _artefactRepository.GetPreviousVersionAsync(
            change.ProjectId, reqFilePath, cancellationToken);

        if (previousArtefact is null)
        {
            return;
        }

        var previousContent = await _artefactStorageService.GetContentAsync(
            previousArtefact.S3Key, cancellationToken);

        if (string.IsNullOrWhiteSpace(previousContent))
        {
            return;
        }

        var nextVersion = await _artefactRepository.GetNextVersionForFileAsync(
            change.ProjectId, reqFilePath, cancellationToken);

        await _artefactStorageService.SaveContentAsync(
            change.ProjectId,
            reqFilePath,
            nextVersion,
            previousContent,
            "text/markdown",
            cancellationToken);
    }
}
