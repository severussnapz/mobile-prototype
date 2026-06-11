using FluentValidation;

namespace Genesis.AI.Domain.Commands.RunPlanningPreflight;

public sealed class RunPlanningPreflightCommandValidator : AbstractValidator<RunPlanningPreflightCommand>
{
    public RunPlanningPreflightCommandValidator()
    {
        RuleFor(command => command.ProjectId)
            .NotEmpty().WithMessage("Project ID is required.");

        RuleFor(command => command.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
