using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using MediatR;

namespace Genesis.AI.Domain.Queries.GetProjectById;

public record GetProjectByIdQuery(Guid ProjectId) : IRequest<Project?>;
