using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Queries.GetProjects;

public class GetProjectsQueryHandler : IRequestHandler<GetProjectsQuery, IReadOnlyList<Project>>
{
    private readonly IProjectRepository _projectRepository;

    public GetProjectsQueryHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
    }

    public async Task<IReadOnlyList<Project>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            return await _projectRepository.GetByStatusAsync(request.Status, cancellationToken);
        }

        return await _projectRepository.GetAllAsync(cancellationToken);
    }
}
