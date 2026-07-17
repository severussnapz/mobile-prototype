using MediatR;

namespace Genesis.AI.Domain.Commands.ReindexProjectArtefacts;

public record ReindexProjectArtefactsCommand(Guid ProjectId, string RequestedBy) : IRequest<ReindexProjectArtefactsResult>;

