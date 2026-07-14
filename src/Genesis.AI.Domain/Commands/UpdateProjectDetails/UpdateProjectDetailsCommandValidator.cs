using FluentValidation;

namespace Genesis.AI.Domain.Commands.UpdateProjectDetails;

public sealed class UpdateProjectDetailsCommandValidator : AbstractValidator<UpdateProjectDetailsCommand>
{
    public UpdateProjectDetailsCommandValidator()
    {
        RuleFor(command => command.ProjectId)
            .NotEmpty();

        RuleFor(command => command.TriggeredBy)
            .NotEmpty();

        RuleFor(command => command.Name)
            .MaximumLength(200)
            .When(command => command.Name is not null);

        RuleFor(command => command.Description)
            .MaximumLength(2000)
            .When(command => command.Description is not null);

        RuleFor(command => command.TimeSheetCode)
            .MaximumLength(50)
            .When(command => command.TimeSheetCode is not null);
    }
}
