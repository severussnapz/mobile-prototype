using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using MediatR;

namespace Genesis.AI.Domain.Commands.DeferParkingLotItem;

public record DeferParkingLotItemCommand(Guid ConversationId, Guid ItemId, string? ClosureDecision = null) : IRequest<DeferParkingLotItemResult>;
