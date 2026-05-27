using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.AdvancePhase;

public class AdvancePhaseCommandHandler : IRequestHandler<AdvancePhaseCommand, AdvancePhaseResult>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IPromptService _promptService;

    public AdvancePhaseCommandHandler(
        IConversationRepository conversationRepository,
        IPromptService promptService)
    {
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
        _promptService = promptService ?? throw new ArgumentNullException(nameof(promptService));
    }

    public async Task<AdvancePhaseResult> Handle(AdvancePhaseCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);
        if (conversation is null)
            return new AdvancePhaseResult(Found: false, ValidationError: null);

        if (conversation.CurrentPhase >= conversation.TotalPhases)
            return new AdvancePhaseResult(Found: true, ValidationError: "Already at final phase");

        var stageType = await _conversationRepository.GetStageTypeByStageIdAsync(conversation.StageId, cancellationToken);
        var phaseNames = stageType is not null ? _promptService.GetPhaseNames(stageType.Value) : ["unknown"];
        var nextPhase = conversation.CurrentPhase + 1;
        var nextPhaseName = nextPhase < phaseNames.Length ? phaseNames[nextPhase] : "unknown";

        conversation.AdvancePhase(nextPhaseName);
        await _conversationRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return new AdvancePhaseResult(Found: true, ValidationError: null, Phase: conversation.CurrentPhase, PhaseName: conversation.PhaseName);
    }
}
