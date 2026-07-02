using FluentValidation;

namespace Genesis.AI.Domain.Commands.GeneratePrototypeDemo;

public class GeneratePrototypeDemoCommandValidator : AbstractValidator<GeneratePrototypeDemoCommand>
{
    public GeneratePrototypeDemoCommandValidator()
    {
        RuleFor(command => command.ProjectId)
            .NotEmpty().WithMessage("Project ID is required.");

        RuleFor(command => command.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
