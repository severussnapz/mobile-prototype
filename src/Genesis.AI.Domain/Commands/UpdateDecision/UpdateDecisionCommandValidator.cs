using FluentValidation;

namespace Genesis.AI.Domain.Commands.UpdateDecision;

public class UpdateDecisionCommandValidator : AbstractValidator<UpdateDecisionCommand>
{
    public UpdateDecisionCommandValidator()
    {
        RuleFor(command => command.ProjectId)
            .NotEmpty().WithMessage("Project ID is required.");

        RuleFor(command => command.DecisionId)
            .NotEmpty().WithMessage("Decision ID is required.");

        RuleFor(command => command.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(command => command.Context)
            .NotEmpty().WithMessage("Context is required.")
            .MaximumLength(10000).WithMessage("Context must not exceed 10000 characters.");

        RuleFor(command => command.Decision)
            .NotEmpty().WithMessage("Decision is required.")
            .MaximumLength(10000).WithMessage("Decision must not exceed 10000 characters.");

        RuleFor(command => command.Consequences)
            .NotEmpty().WithMessage("Consequences are required.")
            .MaximumLength(10000).WithMessage("Consequences must not exceed 10000 characters.");
    }
}
