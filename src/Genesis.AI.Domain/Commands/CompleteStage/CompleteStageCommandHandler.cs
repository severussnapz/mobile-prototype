using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.CompleteStage;

public class CompleteStageCommandHandler : IRequestHandler<CompleteStageCommand, CompleteStageResult>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IArtefactRepository _artefactRepository;
    private readonly TimeProvider _timeProvider;

    public CompleteStageCommandHandler(
        IProjectRepository projectRepository,
        IArtefactRepository artefactRepository,
        TimeProvider timeProvider)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<CompleteStageResult> Handle(CompleteStageCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByStageIdAsync(request.StageId, cancellationToken);
        if (project is null)
            return new CompleteStageResult(Found: false, AlreadyComplete: false, ValidationError: null);

        var stage = project.PipelineStages.First(pipelineStage => pipelineStage.Id == request.StageId);

        if (stage.Status == PipelineStageStatus.Complete)
            return new CompleteStageResult(Found: true, AlreadyComplete: true, ValidationError: null);

        if (stage.Status != PipelineStageStatus.InProgress)
            return new CompleteStageResult(Found: true, AlreadyComplete: false,
                ValidationError: $"Stage must be InProgress to complete. Current status: {stage.Status}");

        var artefacts = await _artefactRepository.GetByProjectIdAsync(project.Id, cancellationToken);
        if (artefacts.Count == 0)
            return new CompleteStageResult(Found: true, AlreadyComplete: false,
                ValidationError: "Cannot complete stage without at least one artefact.");

        stage.Complete(request.UserId, _timeProvider);
        project.RecalculateStatus(_timeProvider);
        await _projectRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return new CompleteStageResult(
            Found: true,
            AlreadyComplete: false,
            ValidationError: null,
            StageId: stage.Id,
            StageType: stage.StageType.ToString(),
            Status: "complete");
    }
}
