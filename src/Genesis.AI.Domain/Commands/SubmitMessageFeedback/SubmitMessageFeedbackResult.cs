using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;

namespace Genesis.AI.Domain.Commands.SubmitMessageFeedback;

public sealed record SubmitMessageFeedbackResult(
    bool IsSuccess,
    bool ConversationExists,
    MessageFeedback? Feedback)
{
    public static SubmitMessageFeedbackResult ConversationNotFound()
    {
        return new SubmitMessageFeedbackResult(false, false, null);
    }

    public static SubmitMessageFeedbackResult AssistantMessageNotFound()
    {
        return new SubmitMessageFeedbackResult(false, true, null);
    }

    public static SubmitMessageFeedbackResult Success(MessageFeedback feedback)
    {
        return new SubmitMessageFeedbackResult(true, true, feedback);
    }
}
