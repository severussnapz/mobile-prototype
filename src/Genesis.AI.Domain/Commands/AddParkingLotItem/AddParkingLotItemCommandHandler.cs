using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.AddParkingLotItem;

public class AddParkingLotItemCommandHandler : IRequestHandler<AddParkingLotItemCommand, AddParkingLotItemResult>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly TimeProvider _timeProvider;

    public AddParkingLotItemCommandHandler(
        IConversationRepository conversationRepository,
        TimeProvider timeProvider)
    {
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<AddParkingLotItemResult> Handle(AddParkingLotItemCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);
        if (conversation is null)
            return new AddParkingLotItemResult(Found: false, ValidationError: null);

        if (!Enum.TryParse<ParkingLotPriority>(request.Priority, true, out var priority))
            return new AddParkingLotItemResult(Found: true, ValidationError: "Invalid priority. Use: critical, high, medium");

        var item = conversation.AddParkingLotItem(request.Content, priority, _timeProvider);
        await _conversationRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return new AddParkingLotItemResult(Found: true, ValidationError: null, Item: item);
    }
}
