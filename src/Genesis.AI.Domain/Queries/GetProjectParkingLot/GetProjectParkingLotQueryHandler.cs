using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Queries.GetProjectParkingLot;

public class GetProjectParkingLotQueryHandler : IRequestHandler<GetProjectParkingLotQuery, IReadOnlyList<ParkingLotItem>?>
{
    private readonly IConversationRepository _conversationRepository;

    public GetProjectParkingLotQueryHandler(IConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
    }

    public async Task<IReadOnlyList<ParkingLotItem>?> Handle(GetProjectParkingLotQuery request, CancellationToken cancellationToken)
    {
        return await _conversationRepository.GetParkingLotByProjectIdAsync(request.ProjectId, cancellationToken);
    }
}
