using MediatR;

namespace Genesis.AI.Domain.Queries.GetArtefactVersions;

public record GetArtefactVersionsQuery(Guid ProjectId, string FilePath) : IRequest<GetArtefactVersionsResult>;
