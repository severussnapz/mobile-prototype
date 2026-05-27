using FluentValidation;

namespace Genesis.AI.Domain.Commands.DeleteProject;

public class DeleteProjectCommandValidator : AbstractValidator<DeleteProjectCommand>
{
    public DeleteProjectCommandValidator()
    {
        RuleFor(command => command.ProjectId)
            .NotEmpty().WithMessage("Project ID is required.");
    }
}
