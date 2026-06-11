using FluentValidation;

namespace Genesis.AI.Domain.Commands.SplitPlanningTasks;

public sealed class SplitPlanningTasksCommandValidator : AbstractValidator<SplitPlanningTasksCommand>
{
    public SplitPlanningTasksCommandValidator()
    {
        RuleFor(command => command.ProjectId)
            .NotEmpty().WithMessage("Project ID is required.");

        RuleFor(command => command.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
