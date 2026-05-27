using FluentValidation;

namespace Genesis.AI.Domain.Commands.SetPhase;

public class SetPhaseCommandValidator : AbstractValidator<SetPhaseCommand>
{
    public SetPhaseCommandValidator()
    {
        RuleFor(command => command.ConversationId)
            .NotEmpty().WithMessage("Conversation ID is required.");

        RuleFor(command => command.Phase)
            .GreaterThan(0).WithMessage("Phase must be greater than zero.");
    }
}
