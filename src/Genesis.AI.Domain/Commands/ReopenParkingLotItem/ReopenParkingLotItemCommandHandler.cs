using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.ReopenParkingLotItem;

public class ReopenParkingLotItemCommandHandler : IRequestHandler<ReopenParkingLotItemCommand, ReopenParkingLotItemResult>
{
    private readonly IConversationRepository _conversationRepository;

    public ReopenParkingLotItemCommandHandler(IConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
    }

    public async Task<ReopenParkingLotItemResult> Handle(ReopenParkingLotItemCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdWithParkingLotAsync(request.ConversationId, cancellationToken);
        if (conversation is null)
            return new ReopenParkingLotItemResult(Found: false);

        var item = conversation.ParkingLotItems.FirstOrDefault(parkingLotItem => parkingLotItem.Id == request.ItemId);
        if (item is null)
            return new ReopenParkingLotItemResult(Found: false);

        item.Reopen();
        await _conversationRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return new ReopenParkingLotItemResult(Found: true, Item: item);
    }
}
