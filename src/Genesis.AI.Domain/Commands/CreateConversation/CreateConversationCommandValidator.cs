using FluentValidation;

namespace Genesis.AI.Domain.Commands.CreateConversation;

public class CreateConversationCommandValidator : AbstractValidator<CreateConversationCommand>
{
    public CreateConversationCommandValidator()
    {
        RuleFor(command => command.StageId)
            .NotEmpty().WithMessage("Stage ID is required.");
    }
}
