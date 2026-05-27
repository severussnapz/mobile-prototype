using MediatR;

namespace Genesis.AI.Domain.Commands.DeleteParkingLotItem;

public record DeleteParkingLotItemCommand(Guid ConversationId, Guid ItemId) : IRequest<bool>;
