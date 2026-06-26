using MediatR;

namespace Genesis.AI.Domain.Commands.SubmitMessageFeedback;

public sealed record SubmitMessageFeedbackCommand(
    Guid ConversationId,
    Guid MessageId,
    bool IsHelpful,
    string? Reason,
    string CreatedBy) : IRequest<SubmitMessageFeedbackResult>;
