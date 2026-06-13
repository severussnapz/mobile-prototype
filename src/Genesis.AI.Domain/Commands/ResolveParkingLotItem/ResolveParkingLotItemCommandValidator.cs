using FluentValidation;

namespace Genesis.AI.Domain.Commands.ResolveParkingLotItem;

public class ResolveParkingLotItemCommandValidator : AbstractValidator<ResolveParkingLotItemCommand>
{
    public ResolveParkingLotItemCommandValidator()
    {
        RuleFor(command => command.ConversationId)
            .NotEmpty().WithMessage("Conversation ID is required.");

        RuleFor(command => command.ItemId)
            .NotEmpty().WithMessage("Item ID is required.");

        RuleFor(command => command.ClosureDecision)
            .NotEmpty().WithMessage("Closure decision is required.")
            .MaximumLength(2000).WithMessage("Closure decision cannot exceed 2000 characters.");
    }
}
