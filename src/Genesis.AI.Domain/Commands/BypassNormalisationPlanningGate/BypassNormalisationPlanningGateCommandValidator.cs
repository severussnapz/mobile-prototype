using FluentValidation;

namespace Genesis.AI.Domain.Commands.BypassNormalisationPlanningGate;

public sealed class BypassNormalisationPlanningGateCommandValidator
    : AbstractValidator<BypassNormalisationPlanningGateCommand>
{
    public BypassNormalisationPlanningGateCommandValidator()
    {
        RuleFor(command => command.ProjectId)
            .NotEmpty().WithMessage("Project ID is required.");

        RuleFor(command => command.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
