using FluentValidation;

namespace Genesis.AI.Domain.Commands.RunLocalNormaliser;

public sealed class RunLocalNormaliserCommandValidator : AbstractValidator<RunLocalNormaliserCommand>
{
    public RunLocalNormaliserCommandValidator()
    {
        RuleFor(command => command.ProjectId)
            .NotEmpty().WithMessage("Project ID is required.");

        RuleFor(command => command.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
