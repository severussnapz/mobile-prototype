using FluentValidation;

namespace Genesis.AI.Domain.Commands.GenerateDpiaReport;

public sealed class GenerateDpiaReportCommandValidator : AbstractValidator<GenerateDpiaReportCommand>
{
    public GenerateDpiaReportCommandValidator()
    {
        RuleFor(command => command.ProjectId)
            .NotEmpty().WithMessage("Project ID is required.");

        RuleFor(command => command.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
