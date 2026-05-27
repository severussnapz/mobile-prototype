using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using MediatR;

namespace Genesis.AI.Domain.Queries.GetProjects;

public record GetProjectsQuery(string? Status) : IRequest<IReadOnlyList<Project>>;
