using FluentValidation;

namespace Genesis.AI.Domain.Commands.AdvancePhase;

public class AdvancePhaseCommandValidator : AbstractValidator<AdvancePhaseCommand>
{
    public AdvancePhaseCommandValidator()
    {
        RuleFor(command => command.ConversationId)
            .NotEmpty().WithMessage("Conversation ID is required.");
    }
}
