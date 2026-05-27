using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using MediatR;

namespace Genesis.AI.Domain.Queries.GetParkingLot;

public record GetParkingLotQuery(Guid ConversationId) : IRequest<IReadOnlyList<ParkingLotItem>?>;
