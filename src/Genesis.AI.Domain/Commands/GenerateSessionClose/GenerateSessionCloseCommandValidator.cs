using FluentValidation;

namespace Genesis.AI.Domain.Commands.GenerateSessionClose;

public sealed class GenerateSessionCloseCommandValidator : AbstractValidator<GenerateSessionCloseCommand>
{
    public GenerateSessionCloseCommandValidator()
    {
        RuleFor(command => command.ProjectId)
            .NotEmpty();

        RuleFor(command => command.ConversationId)
            .NotEmpty();

        RuleFor(command => command.StageType)
            .IsInEnum();

        RuleFor(command => command.UserErn)
            .NotEmpty();
    }
}
