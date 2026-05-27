using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using MediatR;

namespace Genesis.AI.Domain.Commands.ResolveParkingLotItem;

public record ResolveParkingLotItemCommand(Guid ConversationId, Guid ItemId) : IRequest<ParkingLotItemResult>;
