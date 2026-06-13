using FluentValidation;

namespace Genesis.AI.Domain.Commands.ReopenParkingLotItem;

public class ReopenParkingLotItemCommandValidator : AbstractValidator<ReopenParkingLotItemCommand>
{
    public ReopenParkingLotItemCommandValidator()
    {
        RuleFor(command => command.ConversationId)
            .NotEmpty().WithMessage("Conversation ID is required.");

        RuleFor(command => command.ItemId)
            .NotEmpty().WithMessage("Item ID is required.");
    }
}
