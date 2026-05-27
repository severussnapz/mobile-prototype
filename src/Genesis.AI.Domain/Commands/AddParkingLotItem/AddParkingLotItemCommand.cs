using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using MediatR;

namespace Genesis.AI.Domain.Commands.AddParkingLotItem;

public record AddParkingLotItemCommand(Guid ConversationId, string Content, string Priority) : IRequest<AddParkingLotItemResult>;
