using FluentValidation;

namespace Genesis.AI.Domain.Commands.GenerateSecurityReviewReport;

public sealed class GenerateSecurityReviewReportCommandValidator
    : AbstractValidator<GenerateSecurityReviewReportCommand>
{
    public GenerateSecurityReviewReportCommandValidator()
    {
        RuleFor(command => command.ProjectId)
            .NotEmpty().WithMessage("Project ID is required.");

        RuleFor(command => command.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
