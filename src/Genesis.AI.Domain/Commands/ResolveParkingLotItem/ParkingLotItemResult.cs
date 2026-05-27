using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;

namespace Genesis.AI.Domain.Commands.ResolveParkingLotItem;

public record ParkingLotItemResult(bool Found, ParkingLotItem? Item = null);
