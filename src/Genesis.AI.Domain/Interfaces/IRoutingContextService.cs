namespace Genesis.AI.Domain.Interfaces;

/// <summary>
/// Resolves a <see cref="RoutingContext"/> for a given conversation.
/// The context is used by the streaming layer to select the correct system
/// prompt and active skill blocks before invoking AWS Bedrock.
/// </summary>
public interface IRoutingContextService
{
    /// <summary>
    /// Builds a <see cref="RoutingContext"/> from the conversation identified by
    /// <paramref name="conversationId"/>.
    /// </summary>
    /// <param name="conversationId">The conversation to resolve context for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolved <see cref="RoutingContext"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the conversation or its associated stage type cannot be found.
    /// </exception>
    Task<RoutingContext> BuildRoutingContextAsync(Guid conversationId, CancellationToken cancellationToken);
}
