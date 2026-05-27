using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.DeleteParkingLotItem;

public class DeleteParkingLotItemCommandHandler : IRequestHandler<DeleteParkingLotItemCommand, bool>
{
    private readonly IConversationRepository _conversationRepository;

    public DeleteParkingLotItemCommandHandler(IConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
    }

    public async Task<bool> Handle(DeleteParkingLotItemCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdWithParkingLotAsync(request.ConversationId, cancellationToken);
        if (conversation is null) return false;

        var item = conversation.ParkingLotItems.FirstOrDefault(parkingLotItem => parkingLotItem.Id == request.ItemId);
        if (item is null) return false;

        await _conversationRepository.RemoveParkingLotItemAsync(item, cancellationToken);
        return true;
    }
}
