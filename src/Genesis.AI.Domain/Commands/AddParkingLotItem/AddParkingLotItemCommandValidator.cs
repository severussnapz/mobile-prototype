using FluentValidation;

namespace Genesis.AI.Domain.Commands.AddParkingLotItem;

public class AddParkingLotItemCommandValidator : AbstractValidator<AddParkingLotItemCommand>
{
    public AddParkingLotItemCommandValidator()
    {
        RuleFor(command => command.ConversationId)
            .NotEmpty().WithMessage("Conversation ID is required.");

        RuleFor(command => command.Content)
            .NotEmpty().WithMessage("Content is required.")
            .MaximumLength(2000).WithMessage("Content must not exceed 2000 characters.");

        RuleFor(command => command.Priority)
            .NotEmpty().WithMessage("Priority is required.");
    }
}
