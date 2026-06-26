using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.SubmitMessageFeedback;

public sealed class SubmitMessageFeedbackCommandHandler : IRequestHandler<SubmitMessageFeedbackCommand, SubmitMessageFeedbackResult>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageFeedbackRepository _messageFeedbackRepository;
    private readonly TimeProvider _timeProvider;

    public SubmitMessageFeedbackCommandHandler(
        IConversationRepository conversationRepository,
        IMessageFeedbackRepository messageFeedbackRepository,
        TimeProvider timeProvider)
    {
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
        _messageFeedbackRepository = messageFeedbackRepository ?? throw new ArgumentNullException(nameof(messageFeedbackRepository));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SubmitMessageFeedbackResult> Handle(
        SubmitMessageFeedbackCommand request,
        CancellationToken cancellationToken)
    {
        var stageType = await _conversationRepository.GetStageTypeByConversationIdAsync(
            request.ConversationId,
            cancellationToken);

        if (stageType is null)
        {
            return SubmitMessageFeedbackResult.ConversationNotFound();
        }

        var assistantMessageExists = await _messageFeedbackRepository.AssistantMessageExistsAsync(
            request.ConversationId,
            request.MessageId,
            cancellationToken);

        if (!assistantMessageExists)
        {
            return SubmitMessageFeedbackResult.AssistantMessageNotFound();
        }

        var feedback = await _messageFeedbackRepository.GetByMessageAndUserAsync(
            request.MessageId,
            request.CreatedBy,
            cancellationToken);

        if (feedback is null)
        {
            feedback = MessageFeedback.Create(
                request.ConversationId,
                request.MessageId,
                stageType.Value,
                request.IsHelpful,
                request.Reason,
                request.CreatedBy,
                _timeProvider);

            await _messageFeedbackRepository.AddAsync(feedback, cancellationToken);
        }
        else
        {
            feedback.UpdateFeedback(request.IsHelpful, request.Reason, _timeProvider);
        }

        await _messageFeedbackRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
        return SubmitMessageFeedbackResult.Success(feedback);
    }
}
