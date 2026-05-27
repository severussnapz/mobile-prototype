using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.SendMessage;

public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, Guid>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly TimeProvider _timeProvider;

    public SendMessageCommandHandler(
        IConversationRepository conversationRepository,
        TimeProvider timeProvider)
    {
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<Guid> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdWithMessagesAsync(
            request.ConversationId, cancellationToken)
            ?? throw new InvalidOperationException($"Conversation '{request.ConversationId}' not found.");

        var message = conversation.AddMessage(MessageRole.User, request.Content, null, _timeProvider, request.UserErn, request.GivenName, request.FamilyName);

        await _conversationRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return message.Id;
    }
}
