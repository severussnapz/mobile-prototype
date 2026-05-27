using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Queries.GetConversationsByStage;

public class GetConversationsByStageQueryHandler : IRequestHandler<GetConversationsByStageQuery, IReadOnlyList<Conversation>>
{
    private readonly IConversationRepository _conversationRepository;

    public GetConversationsByStageQueryHandler(IConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
    }

    public async Task<IReadOnlyList<Conversation>> Handle(
        GetConversationsByStageQuery request, CancellationToken cancellationToken)
    {
        return await _conversationRepository.GetByStageIdAsync(request.StageId, cancellationToken);
    }
}
