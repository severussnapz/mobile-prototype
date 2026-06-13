using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.CreateConversation;

public class CreateConversationCommandHandler : IRequestHandler<CreateConversationCommand, Guid>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IPromptService _promptService;
    private readonly TimeProvider _timeProvider;

    public CreateConversationCommandHandler(
        IConversationRepository conversationRepository,
        IProjectRepository projectRepository,
        IPromptService promptService,
        TimeProvider timeProvider)
    {
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _promptService = promptService ?? throw new ArgumentNullException(nameof(promptService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<Guid> Handle(CreateConversationCommand request, CancellationToken cancellationToken)
    {
        // Load project with all stages for prerequisite validation
        var project = await _projectRepository.GetByStageIdAsync(request.StageId, cancellationToken)
            ?? throw new InvalidOperationException($"No project found for stage '{request.StageId}'.");

        var targetStage = project.PipelineStages.First(stage => stage.Id == request.StageId);
        var effectiveRequirementId = string.IsNullOrWhiteSpace(request.RequirementId)
            ? null
            : request.RequirementId;

        var isContinuationRequest = request.ContinuedFromConversationId.HasValue;
        if (isContinuationRequest)
        {
            var priorConversation = await _conversationRepository.GetByIdAsync(
                request.ContinuedFromConversationId!.Value,
                cancellationToken);

            if (priorConversation is null)
            {
                throw new InvalidOperationException(
                    $"Continuation source conversation '{request.ContinuedFromConversationId.Value}' was not found.");
            }

            if (priorConversation.StageId != request.StageId)
            {
                throw new InvalidOperationException(
                    "Continuation source conversation stage does not match requested stage.");
            }

            if (effectiveRequirementId is null)
            {
                effectiveRequirementId = priorConversation.RequirementId;
            }
            else if (!string.Equals(
                         effectiveRequirementId,
                         priorConversation.RequirementId,
                         StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Continuation source conversation requirement does not match requested requirement.");
            }
        }

        // Cannot start a Blocked stage
        if (!isContinuationRequest && targetStage.Status == PipelineStageStatus.Blocked)
            throw new InvalidOperationException($"Stage '{targetStage.StageType}' is blocked and cannot be started.");

        // Validate prerequisite stages are complete
        if (!isContinuationRequest)
        {
            ValidatePrerequisites(targetStage, project);
        }

        // Mark stage as InProgress if currently NotStarted or Complete (re-entering)
        if (!isContinuationRequest &&
            targetStage.Status is PipelineStageStatus.NotStarted or PipelineStageStatus.Complete)
        {
            await ActivateStageAsync(project, targetStage, cancellationToken);
        }

        var stageType = targetStage.StageType;
        var totalPhases = _promptService.GetTotalPhases(stageType);

        var conversation = new Conversation(
            request.StageId,
            totalPhases,
            _timeProvider,
            effectiveRequirementId,
            request.ContinuedFromConversationId);

        await _conversationRepository.AddAsync(conversation, cancellationToken);
        await _conversationRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return conversation.Id;
    }

    private async Task ActivateStageAsync(
        Project project,
        PipelineStage stage,
        CancellationToken cancellationToken)
    {
        if (stage.Status == PipelineStageStatus.NotStarted)
            stage.Start(_timeProvider);
        else
            stage.Reopen(_timeProvider);

        project.RecalculateStatus(_timeProvider);
        await _projectRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static void ValidatePrerequisites(PipelineStage targetStage, Project project)
    {
        // Prerequisite chain:
        //   RequirementsDiscovery → Prototype → [Architecture, Design, PxD] → ClinicalSafety → InformationGovernance → Security → Normalisation → Planning
        //
        // - Prototype requires RequirementsDiscovery complete
        // - Architecture/Design/PxD require Prototype complete
        // - ClinicalSafety requires Architecture, Design, AND PxD all complete
        // - InformationGovernance requires Architecture, Design, PxD complete, and ClinicalSafety complete (or blocked)
        // - Security requires InformationGovernance complete
        // - Normalisation requires Security complete
        // - Planning requires Normalisation complete

        switch (targetStage.StageType)
        {
            case StageType.RequirementsDiscovery:
                // No prerequisites
                break;

            case StageType.Prototype:
                RequireStageComplete(project, StageType.RequirementsDiscovery, targetStage.StageType);
                break;

            case StageType.Architecture:
            case StageType.Design:
            case StageType.Pxd:
                RequireStageComplete(project, StageType.Prototype, targetStage.StageType);
                break;

            case StageType.ClinicalSafety:
                RequireClinicalSafetyPrerequisites(project, targetStage.StageType);
                break;

            case StageType.InformationGovernance:
                RequireInformationGovernancePrerequisites(project, targetStage.StageType);
                break;

            case StageType.Security:
                RequireStageComplete(project, StageType.InformationGovernance, targetStage.StageType);
                break;

            case StageType.Normalisation:
                RequireStageComplete(project, StageType.Security, targetStage.StageType);
                break;

            case StageType.Planning:
                RequireStageComplete(project, StageType.Normalisation, targetStage.StageType);
                break;
        }
    }

    private static void RequireClinicalSafetyPrerequisites(Project project, StageType target)
    {
        RequireStageComplete(project, StageType.Architecture, target);
        RequireStageComplete(project, StageType.Design, target);
        RequireStageComplete(project, StageType.Pxd, target);
    }

    private static void RequireInformationGovernancePrerequisites(Project project, StageType target)
    {
        RequireStageComplete(project, StageType.Architecture, target);
        RequireStageComplete(project, StageType.Design, target);
        RequireStageComplete(project, StageType.Pxd, target);
        RequireStageCompleteOrBlocked(project, StageType.ClinicalSafety, target);
    }

    private static void RequireStageComplete(Project project, StageType prerequisite, StageType target)
    {
        var prerequisiteStage = project.PipelineStages
            .FirstOrDefault(stage => stage.StageType == prerequisite);

        if (prerequisiteStage is null || prerequisiteStage.Status != PipelineStageStatus.Complete)
        {
            throw new InvalidOperationException(
                $"Cannot start stage '{target}' because '{prerequisite}' is not yet complete.");
        }
    }

    private static void RequireStageCompleteOrBlocked(Project project, StageType prerequisite, StageType target)
    {
        var prerequisiteStage = project.PipelineStages
            .FirstOrDefault(stage => stage.StageType == prerequisite);

        if (prerequisiteStage is null ||
            (prerequisiteStage.Status != PipelineStageStatus.Complete &&
             prerequisiteStage.Status != PipelineStageStatus.Blocked))
        {
            throw new InvalidOperationException(
                $"Cannot start stage '{target}' because '{prerequisite}' is not yet complete.");
        }
    }
}
