using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Queries.GetProjectById;

public class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, Project?>
{
    private readonly IProjectRepository _projectRepository;

    public GetProjectByIdQueryHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
    }

    public async Task<Project?> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        return await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
    }
}
