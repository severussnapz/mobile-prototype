namespace Genesis.AI.Tests.Api;

public sealed class HelpStreamRequest
{
    public string Message { get; init; } = string.Empty;
    public Guid? ProjectId { get; init; }
    public Guid? HelpConversationId { get; init; }
}
