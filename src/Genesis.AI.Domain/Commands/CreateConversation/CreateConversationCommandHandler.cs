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

        // Cannot start a Blocked stage
        if (targetStage.Status == PipelineStageStatus.Blocked)
            throw new InvalidOperationException($"Stage '{targetStage.StageType}' is blocked and cannot be started.");

        // Validate prerequisite stages are complete
        ValidatePrerequisites(targetStage, project);

        // Mark stage as InProgress if currently NotStarted or Complete (re-entering)
        if (targetStage.Status is PipelineStageStatus.NotStarted or PipelineStageStatus.Complete)
        {
            await ActivateStageAsync(project, targetStage, cancellationToken);
        }

        var stageType = targetStage.StageType;
        var totalPhases = _promptService.GetTotalPhases(stageType);

        var conversation = new Conversation(request.StageId, totalPhases, _timeProvider);

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
        //   RequirementsDiscovery → Prototype → [Architecture, Design, PxD] → ClinicalSafety → Normalisation → Planning
        //
        // - Prototype requires RequirementsDiscovery complete
        // - Architecture/Design/PxD require Prototype complete
        // - ClinicalSafety requires Architecture, Design, AND PxD all complete
        // - Normalisation requires ClinicalSafety complete (or blocked)
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
                RequireStageComplete(project, StageType.Architecture, targetStage.StageType);
                RequireStageComplete(project, StageType.Design, targetStage.StageType);
                RequireStageComplete(project, StageType.Pxd, targetStage.StageType);
                break;

            case StageType.Normalisation:
                RequireStageComplete(project, StageType.Architecture, targetStage.StageType);
                RequireStageComplete(project, StageType.Design, targetStage.StageType);
                RequireStageComplete(project, StageType.Pxd, targetStage.StageType);
                RequireStageCompleteOrBlocked(project, StageType.ClinicalSafety, targetStage.StageType);
                break;

            case StageType.Planning:
                RequireStageComplete(project, StageType.Normalisation, targetStage.StageType);
                break;
        }
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
