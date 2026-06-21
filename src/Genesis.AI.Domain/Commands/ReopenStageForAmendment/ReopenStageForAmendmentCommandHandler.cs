using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Domain.Commands.ReopenStageForAmendment;

public sealed class ReopenStageForAmendmentCommandHandler
{
    private readonly IProjectRepository _projectRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IPromptService _promptService;
    private readonly TimeProvider _timeProvider;

    public ReopenStageForAmendmentCommandHandler(
        IProjectRepository projectRepository,
        IConversationRepository conversationRepository,
        IPromptService promptService,
        TimeProvider timeProvider)
    {
        _projectRepository = projectRepository ??
            throw new ArgumentNullException(nameof(projectRepository));
        _conversationRepository = conversationRepository ??
            throw new ArgumentNullException(nameof(conversationRepository));
        _promptService = promptService ??
            throw new ArgumentNullException(nameof(promptService));
        _timeProvider = timeProvider ??
            throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ReopenStageForAmendmentResult> Handle(
        ReopenStageForAmendmentCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var project = await _projectRepository.GetByStageIdAsync(
            command.StageId, cancellationToken);

        if (project is null)
        {
            return new ReopenStageForAmendmentResult(
                IsSuccess: false,
                ErrorMessage: $"Project for stage '{command.StageId}' not found.");
        }

        var stage = project.PipelineStages.FirstOrDefault(stage => stage.Id == command.StageId);
        if (stage is null)
        {
            return new ReopenStageForAmendmentResult(
                IsSuccess: false,
                ErrorMessage: $"Stage '{command.StageId}' not found on project.");
        }

        stage.Reopen(_timeProvider);
        await _projectRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        var existingConversation = await _conversationRepository
            .GetByStageAndRequirementIdAsync(
                command.StageId, command.ReqId, cancellationToken);

        if (existingConversation is not null)
        {
            return new ReopenStageForAmendmentResult(
                IsSuccess: true,
                ConversationId: existingConversation.Id);
        }

        var totalPhases = _promptService.GetTotalPhases(stage.StageType);
        var conversation = new Conversation(
            command.StageId,
            totalPhases,
            _timeProvider,
            requirementId: command.ReqId);

        await _conversationRepository.AddAsync(conversation, cancellationToken);
        await _conversationRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return new ReopenStageForAmendmentResult(
            IsSuccess: true,
            ConversationId: conversation.Id);
    }
}
