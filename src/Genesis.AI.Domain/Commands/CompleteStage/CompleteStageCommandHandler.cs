using Genesis.AI.Domain.AggregatesModel.RequirementChangeAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.CompleteStage;

public class CompleteStageCommandHandler : IRequestHandler<CompleteStageCommand, CompleteStageResult>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IArtefactRepository _artefactRepository;
    private readonly IRequirementChangeRepository _requirementChangeRepository;
    private readonly TimeProvider _timeProvider;

    public CompleteStageCommandHandler(
        IProjectRepository projectRepository,
        IArtefactRepository artefactRepository,
        IRequirementChangeRepository requirementChangeRepository,
        TimeProvider timeProvider)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _requirementChangeRepository = requirementChangeRepository ?? throw new ArgumentNullException(nameof(requirementChangeRepository));
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
        await RecordDomainReviewsAsync(project.Id, stage.StageType, request.UserId, cancellationToken);

        return new CompleteStageResult(
            Found: true,
            AlreadyComplete: false,
            ValidationError: null,
            StageId: stage.Id,
            StageType: ConvertToSnakeCase(stage.StageType.ToString()),
            Status: "complete");
    }

    private static string ConvertToSnakeCase(string value)
    {
        return string.Concat(value.Select((character, index) =>
            index > 0 && char.IsUpper(character) ? $"_{character}" : character.ToString()))
            .ToLowerInvariant();
    }

    private async Task RecordDomainReviewsAsync(
        Guid projectId,
        StageType stageType,
        string userId,
        CancellationToken cancellationToken)
    {
        Action<RequirementChange, string, TimeProvider>? reviewRecorder = stageType switch
        {
            StageType.ClinicalSafety => static (change, reviewer, timeProvider) =>
            {
                if (change.ClinicalSafetyImpact == ImpactLevel.Definite)
                {
                    change.RecordClinicalSafetyReview(reviewer, timeProvider);
                }
            },
            StageType.InformationGovernance => static (change, reviewer, timeProvider) =>
            {
                if (change.IgImpact == ImpactLevel.Definite)
                {
                    change.RecordIgReview(reviewer, timeProvider);
                }
            },
            StageType.Security => static (change, reviewer, timeProvider) =>
            {
                if (change.SecurityImpact == ImpactLevel.Definite)
                {
                    change.RecordSecurityReview(reviewer, timeProvider);
                }
            },
            _ => null
        };

        if (reviewRecorder is null)
        {
            return;
        }

        var pendingChanges = await _requirementChangeRepository.GetPendingByProjectIdAsync(projectId, cancellationToken);
        var recordedReview = false;

        foreach (var change in pendingChanges)
        {
            var wasReviewed = stageType switch
            {
                StageType.ClinicalSafety => change.ClinicalSafetyReviewed,
                StageType.InformationGovernance => change.IgReviewed,
                StageType.Security => change.SecurityReviewed,
                _ => false
            };

            reviewRecorder(change, userId, _timeProvider);

            var isReviewed = stageType switch
            {
                StageType.ClinicalSafety => change.ClinicalSafetyReviewed,
                StageType.InformationGovernance => change.IgReviewed,
                StageType.Security => change.SecurityReviewed,
                _ => false
            };

            if (!wasReviewed && isReviewed)
            {
                recordedReview = true;
            }
        }

        if (recordedReview)
        {
            await _requirementChangeRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
