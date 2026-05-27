using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using MediatR;

namespace Genesis.AI.Domain.Queries.GetConversationsByStage;

public record GetConversationsByStageQuery(Guid StageId) : IRequest<IReadOnlyList<Conversation>>;
