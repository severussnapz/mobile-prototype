using FluentValidation;

namespace Genesis.AI.Domain.Commands.VerifyCompleteness;

public sealed class VerifyCompletenessCommandValidator : AbstractValidator<VerifyCompletenessCommand>
{
    public VerifyCompletenessCommandValidator()
    {
        RuleFor(command => command.ProjectId)
            .NotEmpty().WithMessage("Project ID is required.");
    }
}
