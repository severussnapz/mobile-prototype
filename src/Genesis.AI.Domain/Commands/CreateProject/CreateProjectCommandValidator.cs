using FluentValidation;

namespace Genesis.AI.Domain.Commands.CreateProject;

public class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("Project name is required.")
            .MaximumLength(200).WithMessage("Project name must not exceed 200 characters.");

        RuleFor(command => command.Code)
            .NotEmpty().WithMessage("Project code is required.")
            .MinimumLength(3).WithMessage("Project code must be at least 3 characters.")
            .MaximumLength(10).WithMessage("Project code must not exceed 10 characters.")
            .Matches("^[A-Z]+$").WithMessage("Project code must contain only uppercase letters.");

        RuleFor(command => command.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

        RuleFor(command => command.TimeSheetCode)
            .NotEmpty().WithMessage("Time sheet code is required.")
            .MaximumLength(50).WithMessage("Time sheet code must not exceed 50 characters.");

        RuleFor(command => command.ComplianceDomain)
            .IsInEnum().WithMessage("Compliance domain must be a valid value.");

        RuleFor(command => command.CreatedBy)
            .NotEmpty().WithMessage("CreatedBy is required.");
    }
}
