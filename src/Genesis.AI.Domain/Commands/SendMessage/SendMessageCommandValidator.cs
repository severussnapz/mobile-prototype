using FluentValidation;

namespace Genesis.AI.Domain.Commands.SendMessage;

public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(command => command.ConversationId)
            .NotEmpty().WithMessage("Conversation ID is required.");

        RuleFor(command => command.Content)
            .NotEmpty().WithMessage("Message content is required.");

        RuleFor(command => command.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
