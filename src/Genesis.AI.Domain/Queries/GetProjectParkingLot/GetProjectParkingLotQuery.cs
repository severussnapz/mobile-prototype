using Genesis.AI.Domain.AggregatesModel.ConversationAggregate;
using MediatR;

namespace Genesis.AI.Domain.Queries.GetProjectParkingLot;

public record GetProjectParkingLotQuery(Guid ProjectId) : IRequest<IReadOnlyList<ParkingLotItem>?>;
