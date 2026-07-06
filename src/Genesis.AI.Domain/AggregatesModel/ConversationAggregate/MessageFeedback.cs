using Genesis.AI.Core.Domain;
using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Domain.AggregatesModel.ConversationAggregate;

public class MessageFeedback : Entity, IAggregateRoot
{
    public Guid ConversationId { get; private set; }
    public Guid MessageId { get; private set; }
    public StageType StageType { get; private set; }
    public bool IsHelpful { get; private set; }
    public string? Reason { get; private set; }
    public string CreatedBy { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private MessageFeedback() { } // Required for EF Core

    public static MessageFeedback Create(
        Guid conversationId,
        Guid messageId,
        StageType stageType,
        bool isHelpful,
        string? reason,
        string createdBy,
        TimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        var now = timeProvider.GetUtcNow();
        return new MessageFeedback
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            MessageId = messageId,
            StageType = stageType,
            IsHelpful = isHelpful,
            Reason = reason,
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateFeedback(bool isHelpful, string? reason, TimeProvider timeProvider)
    {
        IsHelpful = isHelpful;
        Reason = reason;
        UpdatedAt = timeProvider.GetUtcNow();
    }
}