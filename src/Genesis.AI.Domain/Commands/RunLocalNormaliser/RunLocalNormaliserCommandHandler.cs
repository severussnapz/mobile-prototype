using System.Text.Json;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.RunLocalNormaliser;

public sealed class RunLocalNormaliserCommandHandler
    : IRequestHandler<RunLocalNormaliserCommand, RunLocalNormaliserResult>
{
    private const string RunStatusFilePath = "output/NORMALISATION_RUN_STATUS.json";
    private const string JsonContentType = "application/json";

    private readonly IProjectRepository _projectRepository;
    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;
    private readonly INormalisationGateService _normalisationGateService;
    private readonly TimeProvider _timeProvider;

    public RunLocalNormaliserCommandHandler(
        IProjectRepository projectRepository,
        IArtefactRepository artefactRepository,
        IArtefactStorageService artefactStorageService,
        INormalisationGateService normalisationGateService,
        TimeProvider timeProvider)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _artefactStorageService = artefactStorageService ?? throw new ArgumentNullException(nameof(artefactStorageService));
        _normalisationGateService = normalisationGateService ?? throw new ArgumentNullException(nameof(normalisationGateService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RunLocalNormaliserResult> Handle(
        RunLocalNormaliserCommand request,
        CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            return RunLocalNormaliserResult.Failure(
                RunLocalNormaliserStatus.ProjectNotFound,
                $"No project found with ID '{request.ProjectId}'.");
        }

        var evaluation = await _normalisationGateService.EvaluateAsync(
            request.ProjectId,
            project.Code,
            cancellationToken);

        var runStatus = evaluation.RunPrerequisitesMet ? "completed" : "failed";
        var status = evaluation.RunPrerequisitesMet
            ? RunLocalNormaliserStatus.Success
            : RunLocalNormaliserStatus.PrerequisitesMissing;

        await PersistRunStatusAsync(request, runStatus, evaluation, cancellationToken);

        return new RunLocalNormaliserResult(
            status,
            runStatus,
            evaluation.GatePassed,
            evaluation.Errors,
            evaluation.OutputArtefacts,
            status == RunLocalNormaliserStatus.PrerequisitesMissing
                ? "Run prerequisites are missing. See errors for details."
                : null);
    }

    private async Task PersistRunStatusAsync(
        RunLocalNormaliserCommand request,
        string runStatus,
        Domain.Normalisation.NormalisationGateEvaluation evaluation,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new
        {
            action = "run_local_normaliser",
            runStatus,
            gatePassed = evaluation.GatePassed,
            runPrerequisitesMet = evaluation.RunPrerequisitesMet,
            errors = evaluation.Errors,
            recordedAtUtc = _timeProvider.GetUtcNow()
        });

        var existing = await _artefactRepository.GetByProjectAndFilePathAsync(
            request.ProjectId,
            RunStatusFilePath,
            cancellationToken);

        if (existing is not null)
        {
            var nextVersion = existing.Version + 1;
            var existingStorageKey = await _artefactStorageService.SaveContentAsync(
                request.ProjectId,
                RunStatusFilePath,
                nextVersion,
                payload,
                JsonContentType,
                cancellationToken);

            var trackedArtefact = await _artefactRepository.GetByIdAsync(existing.Id, cancellationToken);
            trackedArtefact!.ReplaceContent(
                nextVersion,
                existingStorageKey,
                JsonContentType,
                payload.Length,
                request.UserId,
                _timeProvider);

            await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        var storageKey = await _artefactStorageService.SaveContentAsync(
            request.ProjectId,
            RunStatusFilePath,
            1,
            payload,
            JsonContentType,
            cancellationToken);

        var artefact = Artefact.CreateS3Artefact(
            request.ProjectId,
            1,
            RunStatusFilePath,
            storageKey,
            JsonContentType,
            payload.Length,
            request.UserId,
            _timeProvider);

        await _artefactRepository.AddAsync(artefact, cancellationToken);
        await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }
}
