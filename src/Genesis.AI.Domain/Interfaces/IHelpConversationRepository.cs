using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.HelpChatAggregate;

namespace Genesis.AI.Domain.Interfaces;

public interface IHelpConversationRepository
{
    IUnitOfWork UnitOfWork { get; }

    Task AddAsync(HelpConversation conversation, CancellationToken cancellationToken);

    Task<HelpConversation?> GetByIdWithMessagesAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<HelpConversation?> GetMostRecentByUserAndProjectAsync(
        string userErn,
        Guid? projectId,
        CancellationToken cancellationToken);
}
