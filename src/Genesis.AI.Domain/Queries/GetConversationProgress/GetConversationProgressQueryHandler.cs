using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Queries.GetConversationProgress;

public class GetConversationProgressQueryHandler : IRequestHandler<GetConversationProgressQuery, ConversationProgressResult?>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IPromptService _promptService;

    public GetConversationProgressQueryHandler(
        IConversationRepository conversationRepository,
        IPromptService promptService)
    {
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
        _promptService = promptService ?? throw new ArgumentNullException(nameof(promptService));
    }

    public async Task<ConversationProgressResult?> Handle(GetConversationProgressQuery request, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);
        if (conversation is null) return null;

        var stageType = await _conversationRepository.GetStageTypeByStageIdAsync(conversation.StageId, cancellationToken);
        var phaseNames = stageType is not null ? _promptService.GetPhaseNames(stageType.Value) : ["unknown"];

        return new ConversationProgressResult(
            CurrentPhase: conversation.CurrentPhase,
            PhaseName: conversation.PhaseName,
            TotalPhases: conversation.TotalPhases,
            QuestionsAsked: conversation.QuestionsAsked,
            EstimatedTotalQuestions: conversation.EstimatedTotalQuestions,
            PhaseNames: phaseNames,
            Status: conversation.Status.ToString().ToLowerInvariant());
    }
}
