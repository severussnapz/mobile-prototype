using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Queries.GetParkingLot;

public class GetParkingLotQueryHandler : IRequestHandler<GetParkingLotQuery, IReadOnlyList<ParkingLotItem>?>
{
    private readonly IConversationRepository _conversationRepository;

    public GetParkingLotQueryHandler(IConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
    }

    public async Task<IReadOnlyList<ParkingLotItem>?> Handle(GetParkingLotQuery request, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdWithParkingLotAsync(request.ConversationId, cancellationToken);
        if (conversation is null) return null;

        return conversation.ParkingLotItems.ToList();
    }
}
