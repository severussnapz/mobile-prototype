using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;

namespace Genesis.AI.Domain.Commands.AddParkingLotItem;

public record AddParkingLotItemResult(bool Found, string? ValidationError, ParkingLotItem? Item = null);
