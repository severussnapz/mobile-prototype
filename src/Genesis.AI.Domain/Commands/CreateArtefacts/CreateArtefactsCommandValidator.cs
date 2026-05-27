using FluentValidation;

namespace Genesis.AI.Domain.Commands.CreateArtefacts;

public class CreateArtefactsCommandValidator : AbstractValidator<CreateArtefactsCommand>
{
    public CreateArtefactsCommandValidator()
    {
        RuleFor(command => command.ProjectId)
            .NotEmpty().WithMessage("Project ID is required.");

        RuleFor(command => command.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(command => command.Artefacts)
            .NotEmpty().WithMessage("At least one artefact is required.");
    }
}
