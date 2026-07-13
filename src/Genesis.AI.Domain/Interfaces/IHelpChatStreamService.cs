namespace Genesis.AI.Domain.Interfaces;

public interface IHelpChatStreamService
{
    IAsyncEnumerable<string> StreamAsync(
        string message,
        Guid? projectId,
        Guid? helpConversationId,
        string userErn,
        CancellationToken cancellationToken);

    IAsyncEnumerable<string> StreamAsync<TRequest>(
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : class;
}
