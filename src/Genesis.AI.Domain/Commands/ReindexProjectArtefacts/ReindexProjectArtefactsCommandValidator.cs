using FluentValidation;

namespace Genesis.AI.Domain.Commands.ReindexProjectArtefacts;

public class ReindexProjectArtefactsCommandValidator : AbstractValidator<ReindexProjectArtefactsCommand>
{
    public ReindexProjectArtefactsCommandValidator()
    {
        RuleFor(command => command.ProjectId)
            .NotEmpty().WithMessage("Project ID is required.");

        RuleFor(command => command.RequestedBy)
            .NotEmpty().WithMessage("Requested by is required.");
    }
}
