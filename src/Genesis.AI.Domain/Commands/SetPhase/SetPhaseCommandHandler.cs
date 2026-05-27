using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.SetPhase;

public class SetPhaseCommandHandler : IRequestHandler<SetPhaseCommand, SetPhaseResult>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IPromptService _promptService;

    public SetPhaseCommandHandler(
        IConversationRepository conversationRepository,
        IPromptService promptService)
    {
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
        _promptService = promptService ?? throw new ArgumentNullException(nameof(promptService));
    }

    public async Task<SetPhaseResult> Handle(SetPhaseCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);
        if (conversation is null)
            return new SetPhaseResult(Found: false, ValidationError: null);

        if (request.Phase < 0 || request.Phase > conversation.TotalPhases)
            return new SetPhaseResult(Found: true, ValidationError: "Phase out of range");

        var stageType = await _conversationRepository.GetStageTypeByStageIdAsync(conversation.StageId, cancellationToken);
        var phaseNames = stageType is not null ? _promptService.GetPhaseNames(stageType.Value) : ["unknown"];
        var phaseName = request.Phase < phaseNames.Length ? phaseNames[request.Phase] : "unknown";

        conversation.SetPhase(request.Phase, phaseName);
        await _conversationRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return new SetPhaseResult(Found: true, ValidationError: null, Phase: conversation.CurrentPhase, PhaseName: conversation.PhaseName);
    }
}
