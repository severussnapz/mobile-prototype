using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Queries.GetConversation;

public class GetConversationQueryHandler : IRequestHandler<GetConversationQuery, Conversation?>
{
    private readonly IConversationRepository _conversationRepository;

    public GetConversationQueryHandler(IConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
    }

    public async Task<Conversation?> Handle(GetConversationQuery request, CancellationToken cancellationToken)
    {
        return await _conversationRepository.GetByIdWithMessagesAsync(
            request.ConversationId, cancellationToken);
    }
}
