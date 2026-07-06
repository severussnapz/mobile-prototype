using FluentValidation;

namespace Genesis.AI.Domain.Commands.SubmitMessageFeedback;

public sealed class SubmitMessageFeedbackCommandValidator : AbstractValidator<SubmitMessageFeedbackCommand>
{
    public SubmitMessageFeedbackCommandValidator()
    {
        RuleFor(command => command.ConversationId)
            .NotEmpty().WithMessage("Conversation ID is required.");

        RuleFor(command => command.MessageId)
            .NotEmpty().WithMessage("Message ID is required.");

        RuleFor(command => command.CreatedBy)
            .NotEmpty().WithMessage("CreatedBy is required.");

        RuleFor(command => command.Reason)
            .MaximumLength(4000);
    }
}
