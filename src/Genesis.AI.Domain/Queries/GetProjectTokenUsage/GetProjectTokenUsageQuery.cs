using MediatR;

namespace Genesis.AI.Domain.Queries.GetProjectTokenUsage;

public record GetProjectTokenUsageQuery(Guid ProjectId) : IRequest<ProjectTokenUsageResult>;
