using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;
using System.Linq;

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

        var change = await _repository.GetByIdForProjectAsync(
            command.ChangeId,
            command.ProjectId,
            cancellationToken)
            ?? throw new InvalidOperationException(
                $"Requirement change '{command.ChangeId}' not found.");

        change.Undo(
            undoneBy: command.UndoneBy,
            rationale: command.UndoRationale,
            timeProvider: _timeProvider);

        await RestorePreviousReqVersionAsync(
            change,
            command.UndoneBy,
            cancellationToken);

        await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task RestorePreviousReqVersionAsync(
        RequirementChange change,
        string undoneBy,
        CancellationToken cancellationToken)
    {
        var reqFilePath = await ResolveReqFilePathAsync(change, cancellationToken);

        if (string.IsNullOrWhiteSpace(reqFilePath))
        {
            return;
        }

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

        var storageKey = await _artefactStorageService.SaveContentAsync(
            change.ProjectId,
            reqFilePath,
            nextVersion,
            previousContent,
            "text/markdown",
            cancellationToken);

        var restoredArtefact = Artefact.CreateS3Artefact(
            change.ProjectId,
            nextVersion,
            reqFilePath,
            storageKey,
            "text/markdown",
            System.Text.Encoding.UTF8.GetByteCount(previousContent),
            undoneBy,
            _timeProvider,
            true);

        await _artefactRepository.AddAsync(restoredArtefact, cancellationToken);
    }

    private async Task<string?> ResolveReqFilePathAsync(
        RequirementChange change,
        CancellationToken cancellationToken)
    {
        var legacyPath = $"requirements/{change.ReqId}.md";

        var exactMatch = await _artefactRepository.GetByProjectAndFilePathAsync(
            change.ProjectId,
            legacyPath,
            cancellationToken);

        if (exactMatch is not null)
        {
            return legacyPath;
        }

        var allArtefacts = await _artefactRepository.GetProjectArtefactManifestAsync(
            change.ProjectId,
            cancellationToken);

        var reqPrefix = "requirements/";
        var reqIdPathSegmentPrefix = $"/{change.ReqId}-";
        var reqIdExactSuffix = $"/{change.ReqId}.md";

        var matchedArtefact = allArtefacts.FirstOrDefault(artefact =>
            artefact.FilePath.StartsWith(reqPrefix, StringComparison.OrdinalIgnoreCase) &&
            (artefact.FilePath.Contains(reqIdPathSegmentPrefix, StringComparison.OrdinalIgnoreCase) ||
             artefact.FilePath.EndsWith(reqIdExactSuffix, StringComparison.OrdinalIgnoreCase)));

        return matchedArtefact?.FilePath;
    }
}
