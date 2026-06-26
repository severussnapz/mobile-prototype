using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Genesis.AI.Infrastructure.Repositories;

public class MessageFeedbackRepository : IMessageFeedbackRepository
{
    private readonly GenesisAiDbContext _context;

    public MessageFeedbackRepository(GenesisAiDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IUnitOfWork UnitOfWork => _context;

    public async Task<bool> AssistantMessageExistsAsync(Guid conversationId, Guid messageId, CancellationToken cancellationToken)
    {
        return await _context.Messages.AnyAsync(
            message => message.ConversationId == conversationId
                && message.Id == messageId
                && message.Role == MessageRole.Assistant,
            cancellationToken);
    }

    public async Task<MessageFeedback?> GetByMessageAndUserAsync(Guid messageId, string createdBy, CancellationToken cancellationToken)
    {
        return await _context.Set<MessageFeedback>()
            .FirstOrDefaultAsync(
                feedback => feedback.MessageId == messageId && feedback.CreatedBy == createdBy,
                cancellationToken);
    }

    public async Task AddAsync(MessageFeedback feedback, CancellationToken cancellationToken)
    {
        await _context.Set<MessageFeedback>().AddAsync(feedback, cancellationToken);
    }
}