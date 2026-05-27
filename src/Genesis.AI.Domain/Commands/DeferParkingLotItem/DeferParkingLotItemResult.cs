using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;

namespace Genesis.AI.Domain.Commands.DeferParkingLotItem;

public record DeferParkingLotItemResult(bool Found, ParkingLotItem? Item = null);
