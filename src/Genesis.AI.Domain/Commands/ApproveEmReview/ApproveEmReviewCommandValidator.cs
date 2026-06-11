using FluentValidation;

namespace Genesis.AI.Domain.Commands.ApproveEmReview;

public sealed class ApproveEmReviewCommandValidator : AbstractValidator<ApproveEmReviewCommand>
{
    public ApproveEmReviewCommandValidator()
    {
        RuleFor(command => command.ProjectId)
            .NotEmpty().WithMessage("Project ID is required.");

        RuleFor(command => command.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
