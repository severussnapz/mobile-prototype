using System.Text.Json;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.RunPlanningPreflight;

public sealed class RunPlanningPreflightCommandHandler
    : IRequestHandler<RunPlanningPreflightCommand, RunPlanningPreflightResult>
{
    private const string PreflightStatusFilePath = "output/planning/PREFLIGHT_STATUS.json";
    private const string JsonContentType = "application/json";

    private readonly IProjectRepository _projectRepository;
    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;
    private readonly IPlanningGateService _planningGateService;
    private readonly TimeProvider _timeProvider;

    public RunPlanningPreflightCommandHandler(
        IProjectRepository projectRepository,
        IArtefactRepository artefactRepository,
        IArtefactStorageService artefactStorageService,
        IPlanningGateService planningGateService,
        TimeProvider timeProvider)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _artefactStorageService = artefactStorageService ?? throw new ArgumentNullException(nameof(artefactStorageService));
        _planningGateService = planningGateService ?? throw new ArgumentNullException(nameof(planningGateService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RunPlanningPreflightResult> Handle(
        RunPlanningPreflightCommand request,
        CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            return new RunPlanningPreflightResult(
                RunPlanningPreflightStatus.ProjectNotFound,
                false,
                [],
                [],
                $"No project found with ID '{request.ProjectId}'.");
        }

        var evaluation = await _planningGateService.EvaluateAsync(request.ProjectId, cancellationToken);

        var preflightStatus = evaluation.PreflightPassed ? "passed" : "failed";

        var payload = JsonSerializer.Serialize(new
        {
            status = preflightStatus,
            errors = evaluation.Errors,
            recordedAtUtc = _timeProvider.GetUtcNow()
        });

        await PersistArtefactAsync(request, PreflightStatusFilePath, payload, cancellationToken);

        return new RunPlanningPreflightResult(
            RunPlanningPreflightStatus.Success,
            evaluation.PreflightPassed,
            evaluation.Errors,
            evaluation.OutputArtefacts,
            null);
    }

    private async Task PersistArtefactAsync(
        RunPlanningPreflightCommand request,
        string filePath,
        string payload,
        CancellationToken cancellationToken)
    {
        var existing = await _artefactRepository.GetByProjectAndFilePathAsync(
            request.ProjectId,
            filePath,
            cancellationToken);

        if (existing is not null)
        {
            var nextVersion = existing.Version + 1;
            var storageKey = await _artefactStorageService.SaveContentAsync(
                request.ProjectId,
                filePath,
                nextVersion,
                payload,
                JsonContentType,
                cancellationToken);

            var tracked = await _artefactRepository.GetByIdAsync(existing.Id, cancellationToken);
            tracked!.ReplaceContent(nextVersion, storageKey, JsonContentType, payload.Length, request.UserId, _timeProvider);
            await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        var newStorageKey = await _artefactStorageService.SaveContentAsync(
            request.ProjectId,
            filePath,
            1,
            payload,
            JsonContentType,
            cancellationToken);

        var artefact = Artefact.CreateS3Artefact(
            request.ProjectId,
            1,
            filePath,
            newStorageKey,
            JsonContentType,
            payload.Length,
            request.UserId,
            _timeProvider);

        await _artefactRepository.AddAsync(artefact, cancellationToken);
        await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }
}
