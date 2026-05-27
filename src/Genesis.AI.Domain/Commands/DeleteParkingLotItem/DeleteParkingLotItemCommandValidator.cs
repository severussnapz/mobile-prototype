using FluentValidation;

namespace Genesis.AI.Domain.Commands.DeleteParkingLotItem;

public class DeleteParkingLotItemCommandValidator : AbstractValidator<DeleteParkingLotItemCommand>
{
    public DeleteParkingLotItemCommandValidator()
    {
        RuleFor(command => command.ConversationId)
            .NotEmpty().WithMessage("Conversation ID is required.");

        RuleFor(command => command.ItemId)
            .NotEmpty().WithMessage("Item ID is required.");
    }
}
