using FluentValidation;

namespace Genesis.AI.Domain.Commands.CompleteStage;

public class CompleteStageCommandValidator : AbstractValidator<CompleteStageCommand>
{
    public CompleteStageCommandValidator()
    {
        RuleFor(command => command.StageId)
            .NotEmpty().WithMessage("Stage ID is required.");

        RuleFor(command => command.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
