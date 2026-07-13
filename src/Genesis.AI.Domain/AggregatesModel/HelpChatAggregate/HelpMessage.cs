namespace Genesis.AI.Domain.AggregatesModel.HelpChatAggregate;

public sealed class HelpMessage
{
    public Guid Id { get; private set; }
    public Guid HelpConversationId { get; private set; }
    public string Role { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    private HelpMessage() { } // Required for EF Core

    internal static HelpMessage Create(
        Guid helpConversationId,
        string role,
        string content,
        TimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(role);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        return new HelpMessage
        {
            Id = Guid.NewGuid(),
            HelpConversationId = helpConversationId,
            Role = role,
            Content = content,
            CreatedAt = timeProvider.GetUtcNow()
        };
    }
}
