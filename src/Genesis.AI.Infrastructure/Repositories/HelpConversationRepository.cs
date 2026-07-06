using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.HelpChatAggregate;
using Genesis.AI.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Genesis.AI.Infrastructure.Repositories;

public class HelpConversationRepository : IHelpConversationRepository
{
    private readonly GenesisAiDbContext _context;

    public HelpConversationRepository(GenesisAiDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IUnitOfWork UnitOfWork => _context;

    public async Task AddAsync(HelpConversation conversation, CancellationToken cancellationToken)
    {
        await _context.HelpConversation.AddAsync(conversation, cancellationToken);
    }

    public async Task<HelpConversation?> GetByIdWithMessagesAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.HelpConversation
            .Include(conversation => conversation.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(conversation => conversation.Id == id, cancellationToken);
    }

    public async Task<HelpConversation?> GetMostRecentByUserAndProjectAsync(
        string userErn,
        Guid? projectId,
        CancellationToken cancellationToken)
    {
        return await _context.HelpConversation
            .Where(conversation => conversation.UserErn == userErn
                && (projectId == null ? conversation.ProjectId == null : conversation.ProjectId == projectId))
            .OrderByDescending(conversation => conversation.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
