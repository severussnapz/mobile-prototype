using Genesis.AI.Domain.AggregatesModel.ProjectDecisionAggregate;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Queries.GetProjectDecisions;

public class GetProjectDecisionsQueryHandler : IRequestHandler<GetProjectDecisionsQuery, IReadOnlyList<ProjectDecision>?>
{
    private readonly IProjectDecisionRepository _decisionRepository;
    private readonly IProjectRepository _projectRepository;

    public GetProjectDecisionsQueryHandler(
        IProjectDecisionRepository decisionRepository,
        IProjectRepository projectRepository)
    {
        _decisionRepository = decisionRepository ?? throw new ArgumentNullException(nameof(decisionRepository));
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
    }

    public async Task<IReadOnlyList<ProjectDecision>?> Handle(GetProjectDecisionsQuery request, CancellationToken cancellationToken)
    {
        var projectExists = await _projectRepository.ExistsAsync(request.ProjectId, cancellationToken);
        if (!projectExists)
            return null;

        return await _decisionRepository.GetByProjectIdAsync(request.ProjectId, cancellationToken);
    }
}
