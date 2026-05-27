using Genesis.AI.Core.Data;
using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Genesis.AI.Infrastructure.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly GenesisAiDbContext _context;

    public ConversationRepository(GenesisAiDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IUnitOfWork UnitOfWork => _context;

    public async Task AddAsync(Conversation conversation, CancellationToken cancellationToken)
    {
        await _context.Conversations.AddAsync(conversation, cancellationToken);
    }

    public async Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Conversations
            .FirstOrDefaultAsync(conversation => conversation.Id == id, cancellationToken);
    }

    public async Task<Conversation?> GetByIdWithMessagesAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Conversations
            .Include(conversation => conversation.Messages)
            .Include(conversation => conversation.TokenUsageRecords)
            .FirstOrDefaultAsync(conversation => conversation.Id == id, cancellationToken);
    }

    public async Task<Conversation?> GetByIdWithParkingLotAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Conversations
            .Include(conversation => conversation.ParkingLotItems)
            .FirstOrDefaultAsync(conversation => conversation.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Conversation>> GetByStageIdAsync(Guid stageId, CancellationToken cancellationToken)
    {
        return await _context.Conversations
            .AsNoTracking()
            .Where(conversation => conversation.StageId == stageId)
            .OrderByDescending(conversation => conversation.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<StageType?> GetStageTypeByStageIdAsync(Guid stageId, CancellationToken cancellationToken)
    {
        return await _context.PipelineStages
            .Where(stage => stage.Id == stageId)
            .Select(stage => (StageType?)stage.StageType)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<StageType?> GetStageTypeByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        return await _context.Conversations
            .Where(conversation => conversation.Id == conversationId)
            .Join(_context.PipelineStages, conversation => conversation.StageId, stage => stage.Id, (_, stage) => (StageType?)stage.StageType)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProjectContext?> GetProjectContextByStageIdAsync(Guid stageId, CancellationToken cancellationToken)
    {
        return await _context.PipelineStages
            .Where(stage => stage.Id == stageId)
            .Join(_context.Projects, stage => stage.ProjectId, project => project.Id, (_, project) => new ProjectContext(
                project.Id,
                project.Code,
                project.Name,
                project.Description,
                project.ComplianceDomain))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ParkingLotItem>> GetParkingLotByProjectIdAsync(Guid projectId, CancellationToken cancellationToken)
    {
        return await _context.ParkingLotItems
            .AsNoTracking()
            .Join(_context.Conversations, item => item.ConversationId, conversation => conversation.Id, (item, conversation) => new { item, conversation.StageId })
            .Join(_context.PipelineStages, joined => joined.StageId, stage => stage.Id, (joined, stage) => new { joined.item, stage.ProjectId })
            .Where(joined => joined.ProjectId == projectId)
            .Select(joined => joined.item)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task RemoveParkingLotItemAsync(ParkingLotItem item, CancellationToken cancellationToken)
    {
        _context.ParkingLotItems.Remove(item);
        return _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StageTokenUsageSummary>> GetTokenUsageByProjectIdAsync(Guid projectId, CancellationToken cancellationToken)
    {
        return await _context.TokenUsageRecords
            .AsNoTracking()
            .Join(_context.Conversations, record => record.ConversationId, conversation => conversation.Id, (record, conversation) => new { record, conversation.StageId })
            .Join(_context.PipelineStages, joined => joined.StageId, stage => stage.Id, (joined, stage) => new { joined.record, stage.ProjectId, stage.Id, stage.StageType })
            .Where(joined => joined.ProjectId == projectId)
            .GroupBy(joined => new { StageId = joined.Id, joined.StageType })
            .Select(group => new StageTokenUsageSummary(
                group.Key.StageId,
                group.Key.StageType,
                group.Sum(item => item.record.InputTokens),
                group.Sum(item => item.record.OutputTokens),
                group.Sum(item => item.record.CacheReadInputTokens),
                group.Sum(item => item.record.CacheWriteInputTokens),
                group.Count()))
            .ToListAsync(cancellationToken);
    }
}
