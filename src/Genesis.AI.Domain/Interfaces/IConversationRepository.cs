using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Domain.Interfaces;

public interface IConversationRepository
{
    IUnitOfWork UnitOfWork { get; }

    Task AddAsync(Conversation conversation, CancellationToken cancellationToken);
    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Conversation?> GetByIdWithMessagesAsync(Guid id, CancellationToken cancellationToken);
    Task<Conversation?> GetByIdWithParkingLotAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Conversation>> GetByStageIdAsync(Guid stageId, CancellationToken cancellationToken);
    Task<StageType?> GetStageTypeByStageIdAsync(Guid stageId, CancellationToken cancellationToken);
    Task<StageType?> GetStageTypeByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken);
    Task<ProjectContext?> GetProjectContextByStageIdAsync(Guid stageId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ParkingLotItem>> GetParkingLotByProjectIdAsync(Guid projectId, CancellationToken cancellationToken);
    Task RemoveParkingLotItemAsync(ParkingLotItem item, CancellationToken cancellationToken);
    Task<IReadOnlyList<StageTokenUsageSummary>> GetTokenUsageByProjectIdAsync(Guid projectId, CancellationToken cancellationToken);
}
