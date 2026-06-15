using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;

namespace Genesis.AI.Domain.Commands.ReopenParkingLotItem;

public record ReopenParkingLotItemResult(bool Found, ParkingLotItem? Item = null);
