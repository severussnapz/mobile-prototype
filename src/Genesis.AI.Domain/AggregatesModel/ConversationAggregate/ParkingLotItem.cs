using Genesis.AI.Core.Domain;
using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Domain.AggregatesModel.ConversationAggregate;

public class ParkingLotItem : Entity
{
    public Guid ConversationId { get; private set; }
    public string Content { get; private set; } = null!;
    public ParkingLotPriority Priority { get; private set; }
    public ParkingLotStatus Status { get; private set; }
    public int SourcePhase { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private ParkingLotItem() { } // Required for EF Core

    public ParkingLotItem(
        Guid conversationId,
        string content,
        ParkingLotPriority priority,
        int sourcePhase,
        TimeProvider timeProvider)
    {
        Id = Guid.NewGuid();
        ConversationId = conversationId;
        Content = content;
        Priority = priority;
        Status = ParkingLotStatus.Open;
        SourcePhase = sourcePhase;
        CreatedAt = timeProvider.GetUtcNow();
    }

    public void Resolve(TimeProvider timeProvider)
    {
        Status = ParkingLotStatus.Resolved;
        ResolvedAt = timeProvider.GetUtcNow();
    }

    public void Defer()
    {
        Status = ParkingLotStatus.Deferred;
    }

    public void UpdatePriority(ParkingLotPriority priority)
    {
        Priority = priority;
    }

    public void UpdateContent(string content)
    {
        Content = content;
    }
}
