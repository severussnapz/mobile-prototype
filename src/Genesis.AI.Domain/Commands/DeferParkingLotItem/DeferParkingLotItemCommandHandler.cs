using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.DeferParkingLotItem;

public class DeferParkingLotItemCommandHandler : IRequestHandler<DeferParkingLotItemCommand, DeferParkingLotItemResult>
{
    private readonly IConversationRepository _conversationRepository;

    public DeferParkingLotItemCommandHandler(IConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
    }

    public async Task<DeferParkingLotItemResult> Handle(DeferParkingLotItemCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdWithParkingLotAsync(request.ConversationId, cancellationToken);
        if (conversation is null)
            return new DeferParkingLotItemResult(Found: false);

        var item = conversation.ParkingLotItems.FirstOrDefault(parkingLotItem => parkingLotItem.Id == request.ItemId);
        if (item is null)
            return new DeferParkingLotItemResult(Found: false);

        item.Defer();
        await _conversationRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return new DeferParkingLotItemResult(Found: true, Item: item);
    }
}
