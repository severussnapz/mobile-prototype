using System.Text.Json;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.BypassNormalisationPlanningGate;

public sealed class BypassNormalisationPlanningGateCommandHandler
    : IRequestHandler<BypassNormalisationPlanningGateCommand, BypassNormalisationPlanningGateResult>
{
    private const string BypassAuditFilePath = "output/NORMALISATION_BYPASS_AUDIT.json";
    private const string JsonContentType = "application/json";

    private readonly IProjectRepository _projectRepository;
    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;
    private readonly TimeProvider _timeProvider;

    public BypassNormalisationPlanningGateCommandHandler(
        IProjectRepository projectRepository,
        IArtefactRepository artefactRepository,
        IArtefactStorageService artefactStorageService,
        TimeProvider timeProvider)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _artefactStorageService = artefactStorageService ?? throw new ArgumentNullException(nameof(artefactStorageService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<BypassNormalisationPlanningGateResult> Handle(
        BypassNormalisationPlanningGateCommand request,
        CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            return new BypassNormalisationPlanningGateResult(
                BypassNormalisationPlanningGateStatus.ProjectNotFound,
                $"No project found with ID '{request.ProjectId}'.");
        }

        var bypassedAtUtc = _timeProvider.GetUtcNow();
        var payload = JsonSerializer.Serialize(new
        {
            activeBypass = new
            {
                bypassedBy = request.UserId,
                bypassedAtUtc,
                reason = request.Reason
            }
        });

        var existing = await _artefactRepository.GetByProjectAndFilePathAsync(
            request.ProjectId,
            BypassAuditFilePath,
            cancellationToken);

        if (existing is not null)
        {
            var nextVersion = existing.Version + 1;
            var existingStorageKey = await _artefactStorageService.SaveContentAsync(
                request.ProjectId,
                BypassAuditFilePath,
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
            return new BypassNormalisationPlanningGateResult(BypassNormalisationPlanningGateStatus.Success, null);
        }

        var storageKey = await _artefactStorageService.SaveContentAsync(
            request.ProjectId,
            BypassAuditFilePath,
            1,
            payload,
            JsonContentType,
            cancellationToken);

        var artefact = Artefact.CreateS3Artefact(
            request.ProjectId,
            1,
            BypassAuditFilePath,
            storageKey,
            JsonContentType,
            payload.Length,
            request.UserId,
            _timeProvider);

        await _artefactRepository.AddAsync(artefact, cancellationToken);
        await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return new BypassNormalisationPlanningGateResult(BypassNormalisationPlanningGateStatus.Success, null);
    }
}
