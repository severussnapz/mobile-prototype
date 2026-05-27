using FluentValidation;

namespace Genesis.AI.Domain.Commands.SkipStage;

public class SkipStageCommandValidator : AbstractValidator<SkipStageCommand>
{
    public SkipStageCommandValidator()
    {
        RuleFor(command => command.StageId)
            .NotEmpty().WithMessage("Stage ID is required.");
    }
}
