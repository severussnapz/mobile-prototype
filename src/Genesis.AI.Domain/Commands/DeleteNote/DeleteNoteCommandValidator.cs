using FluentValidation;

namespace Genesis.AI.Domain.Commands.DeleteNote;

public class DeleteNoteCommandValidator : AbstractValidator<DeleteNoteCommand>
{
    public DeleteNoteCommandValidator()
    {
        RuleFor(command => command.ProjectId)
            .NotEmpty().WithMessage("Project ID is required.");

        RuleFor(command => command.NoteId)
            .NotEmpty().WithMessage("Note ID is required.");
    }
}
