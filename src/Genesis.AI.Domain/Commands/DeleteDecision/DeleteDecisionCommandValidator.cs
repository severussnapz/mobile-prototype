using FluentValidation;

namespace Genesis.AI.Domain.Commands.DeleteDecision;

public class DeleteDecisionCommandValidator : AbstractValidator<DeleteDecisionCommand>
{
    public DeleteDecisionCommandValidator()
    {
        RuleFor(command => command.ProjectId)
            .NotEmpty().WithMessage("Project ID is required.");

        RuleFor(command => command.DecisionId)
            .NotEmpty().WithMessage("Decision ID is required.");
    }
}
