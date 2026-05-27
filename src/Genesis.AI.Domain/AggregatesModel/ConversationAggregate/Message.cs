using Genesis.AI.Core.Domain;
using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Domain.AggregatesModel.ConversationAggregate;

public class Message : Entity
{
    public Guid ConversationId { get; private set; }
    public MessageRole Role { get; private set; }
    public string Content { get; private set; } = null!;
    public int? TokenCount { get; private set; }
    public string? UserErn { get; private set; }
    public string? GivenName { get; private set; }
    public string? FamilyName { get; private set; }
    public List<MessageImage>? Images { get; private set; }
    public List<MessageDocument>? Documents { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Message() { } // Required for EF Core

    internal Message(Guid conversationId, MessageRole role, string content, int? tokenCount, TimeProvider timeProvider, string? userErn = null, string? givenName = null, string? familyName = null, List<MessageImage>? images = null, List<MessageDocument>? documents = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        Id = Guid.NewGuid();
        ConversationId = conversationId;
        Role = role;
        Content = content;
        TokenCount = tokenCount;
        UserErn = userErn;
        GivenName = givenName;
        FamilyName = familyName;
        Images = images;
        Documents = documents;
        CreatedAt = timeProvider.GetUtcNow();
    }
}
