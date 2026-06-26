using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;

namespace Genesis.AI.Domain.Interfaces;

public interface IMessageFeedbackRepository
{
    IUnitOfWork UnitOfWork { get; }

    Task<bool> AssistantMessageExistsAsync(Guid conversationId, Guid messageId, CancellationToken cancellationToken);
    Task<MessageFeedback?> GetByMessageAndUserAsync(Guid messageId, string createdBy, CancellationToken cancellationToken);
    Task AddAsync(MessageFeedback feedback, CancellationToken cancellationToken);
}