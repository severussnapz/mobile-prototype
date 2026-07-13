using Genesis.AI.Core.Domain;

namespace Genesis.AI.Domain.AggregatesModel.HelpChatAggregate;

public sealed class HelpConversation : Entity, IAggregateRoot
{
    public Guid? ProjectId { get; private set; }
    public string UserErn { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private readonly List<HelpMessage> _messages = [];
    public IReadOnlyCollection<HelpMessage> Messages => _messages.AsReadOnly();

    private HelpConversation() { } // Required for EF Core

    public static HelpConversation Create(
        Guid? projectId,
        string userErn,
        TimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userErn);

        var now = timeProvider.GetUtcNow();
        return new HelpConversation
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserErn = userErn,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public HelpMessage AddMessage(string role, string content, TimeProvider timeProvider)
    {
        var message = HelpMessage.Create(Id, role, content, timeProvider);
        _messages.Add(message);
        UpdatedAt = timeProvider.GetUtcNow();
        return message;
    }
}
