using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using MediatR;

namespace Genesis.AI.Domain.Queries.GetConversation;

public record GetConversationQuery(Guid ConversationId) : IRequest<Conversation?>;
