using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using MediatR;

namespace Genesis.AI.Domain.Commands.ReopenParkingLotItem;

public record ReopenParkingLotItemCommand(Guid ConversationId, Guid ItemId) : IRequest<ReopenParkingLotItemResult>;
