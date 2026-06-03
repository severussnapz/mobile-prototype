namespace Genesis.AI.Api.Features.Conversations;

public sealed class ParkingLotItemResponse
{
    public Guid Id { get; init; }
    public Guid ConversationId { get; init; }
    public string Content { get; init; } = null!;
    public string Priority { get; init; } = null!;
    public string Status { get; init; } = null!;
    public int SourcePhase { get; init; }
    public DateTimeOffset? ResolvedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
