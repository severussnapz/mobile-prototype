using FluentValidation;

namespace Genesis.AI.Domain.Commands.SavePrototypeDemoHtml;

public sealed class SavePrototypeDemoHtmlCommandValidator : AbstractValidator<SavePrototypeDemoHtmlCommand>
{
    public SavePrototypeDemoHtmlCommandValidator()
    {
        RuleFor(command => command.ProjectId)
            .NotEmpty().WithMessage("Project ID is required.");

        RuleFor(command => command.Html)
            .NotEmpty().WithMessage("HTML content is required.");

        RuleFor(command => command.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
