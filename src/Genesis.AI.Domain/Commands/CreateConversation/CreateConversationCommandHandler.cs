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
        var project = await GetProjectForStageAsync(request.StageId, cancellationToken);
        var targetStage = project.PipelineStages.First(stage => stage.Id == request.StageId);
        var effectiveRequirementId = await ResolveEffectiveRequirementIdAsync(request, cancellationToken);

        EnsureStageCanStart(project, targetStage, request.ContinuedFromConversationId.HasValue);
        await ActivateStageIfRequiredAsync(project, targetStage, request.ContinuedFromConversationId.HasValue, cancellationToken);

        var conversation = CreateConversation(request, targetStage.StageType, effectiveRequirementId);
        await SaveConversationAsync(conversation, cancellationToken);

        return conversation.Id;
    }

    private async Task<Project> GetProjectForStageAsync(Guid stageId, CancellationToken cancellationToken)
    {
        return await _projectRepository.GetByStageIdAsync(stageId, cancellationToken)
            ?? throw new InvalidOperationException($"No project found for stage '{stageId}'.");
    }

    private async Task<string?> ResolveEffectiveRequirementIdAsync(
        CreateConversationCommand request,
        CancellationToken cancellationToken)
    {
        var effectiveRequirementId = string.IsNullOrWhiteSpace(request.RequirementId)
            ? null
            : request.RequirementId;

        if (!request.ContinuedFromConversationId.HasValue)
        {
            return effectiveRequirementId;
        }

        var priorConversation = await GetPriorConversationAsync(
            request.ContinuedFromConversationId.Value,
            cancellationToken);

        ValidateContinuationStage(request.StageId, priorConversation.StageId);
        return ResolveContinuationRequirementId(effectiveRequirementId, priorConversation.RequirementId);
    }

    private async Task<Conversation> GetPriorConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        return await _conversationRepository.GetByIdAsync(conversationId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Continuation source conversation '{conversationId}' was not found.");
    }

    private static void ValidateContinuationStage(Guid requestedStageId, Guid priorStageId)
    {
        if (priorStageId != requestedStageId)
        {
            throw new InvalidOperationException(
                "Continuation source conversation stage does not match requested stage.");
        }
    }

    private static string? ResolveContinuationRequirementId(string? requestedRequirementId, string? priorRequirementId)
    {
        if (requestedRequirementId is null)
        {
            return priorRequirementId;
        }

        if (!string.Equals(requestedRequirementId, priorRequirementId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Continuation source conversation requirement does not match requested requirement.");
        }

        return requestedRequirementId;
    }

    private static void EnsureStageCanStart(Project project, PipelineStage targetStage, bool isContinuationRequest)
    {
        if (isContinuationRequest)
        {
            return;
        }

        if (targetStage.Status == PipelineStageStatus.Blocked)
        {
            throw new InvalidOperationException($"Stage '{targetStage.StageType}' is blocked and cannot be started.");
        }

        ValidatePrerequisites(targetStage, project);
    }

    private async Task ActivateStageIfRequiredAsync(
        Project project,
        PipelineStage targetStage,
        bool isContinuationRequest,
        CancellationToken cancellationToken)
    {
        if (isContinuationRequest ||
            targetStage.Status is not (PipelineStageStatus.NotStarted or PipelineStageStatus.Complete))
        {
            return;
        }

        await ActivateStageAsync(project, targetStage, cancellationToken);
    }

    private Conversation CreateConversation(
        CreateConversationCommand request,
        StageType stageType,
        string? effectiveRequirementId)
    {
        var totalPhases = _promptService.GetTotalPhases(stageType);

        return new Conversation(
            request.StageId,
            totalPhases,
            _timeProvider,
            effectiveRequirementId,
            request.ContinuedFromConversationId);
    }

    private async Task SaveConversationAsync(Conversation conversation, CancellationToken cancellationToken)
    {
        await _conversationRepository.AddAsync(conversation, cancellationToken);
        await _conversationRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
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
