using MediatR;

namespace Genesis.AI.Domain.Queries.GetConversationProgress;

public record GetConversationProgressQuery(Guid ConversationId) : IRequest<ConversationProgressResult?>;
