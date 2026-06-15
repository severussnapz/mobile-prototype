using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.ResolveParkingLotItem;

public class ResolveParkingLotItemCommandHandler : IRequestHandler<ResolveParkingLotItemCommand, ParkingLotItemResult>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly TimeProvider _timeProvider;

    public ResolveParkingLotItemCommandHandler(
        IConversationRepository conversationRepository,
        TimeProvider timeProvider)
    {
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ParkingLotItemResult> Handle(ResolveParkingLotItemCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdWithParkingLotAsync(request.ConversationId, cancellationToken);
        if (conversation is null)
            return new ParkingLotItemResult(Found: false);

        var item = conversation.ParkingLotItems.FirstOrDefault(parkingLotItem => parkingLotItem.Id == request.ItemId);
        if (item is null)
            return new ParkingLotItemResult(Found: false);

        item.Resolve(_timeProvider, request.ClosureDecision);
        await _conversationRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return new ParkingLotItemResult(Found: true, Item: item);
    }
}
