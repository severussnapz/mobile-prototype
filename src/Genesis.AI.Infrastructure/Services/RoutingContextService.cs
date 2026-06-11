using Genesis.AI.Domain;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

/// <summary>
/// Resolves a <see cref="RoutingContext"/> by loading the conversation and its
/// associated stage type from the repository.
///
/// Both queries run in parallel to minimise latency. Throws
/// <see cref="InvalidOperationException"/> if either the conversation or the
/// stage type is missing — callers should treat this as a 404-equivalent.
/// </summary>
public sealed class RoutingContextService : IRoutingContextService
{
    private readonly IConversationRepository _conversationRepository;

    public RoutingContextService(IConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository;
    }

    /// <inheritdoc />
    public async Task<RoutingContext> BuildRoutingContextAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        var conversationTask = _conversationRepository.GetByIdAsync(conversationId, cancellationToken);
        var stageTypeTask = _conversationRepository.GetStageTypeByConversationIdAsync(conversationId, cancellationToken);

        await Task.WhenAll(conversationTask, stageTypeTask);

        var conversation = await conversationTask
            ?? throw new InvalidOperationException($"Conversation '{conversationId}' not found.");

        var stageType = await stageTypeTask
            ?? throw new InvalidOperationException($"Stage type not found for conversation '{conversationId}'.");

        var isFirstMessage = conversation.QuestionsAsked <= 1;

        return new RoutingContext(stageType, conversation.CurrentPhase, isFirstMessage);
    }
}
