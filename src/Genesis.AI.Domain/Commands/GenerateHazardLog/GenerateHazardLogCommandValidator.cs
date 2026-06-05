using FluentValidation;

namespace Genesis.AI.Domain.Commands.GenerateHazardLog;

public class GenerateHazardLogCommandValidator : AbstractValidator<GenerateHazardLogCommand>
{
    public GenerateHazardLogCommandValidator()
    {
        RuleFor(command => command.ProjectId)
            .NotEmpty().WithMessage("Project ID is required.");

        RuleFor(command => command.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
