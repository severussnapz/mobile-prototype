using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Domain.Planning;
using MediatR;

namespace Genesis.AI.Domain.Queries.GetPlanningArtefacts;

public sealed class GetPlanningArtefactsQueryHandler
    : IRequestHandler<GetPlanningArtefactsQuery, GetPlanningArtefactsResult>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IArtefactRepository _artefactRepository;

    public GetPlanningArtefactsQueryHandler(
        IProjectRepository projectRepository,
        IArtefactRepository artefactRepository)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
    }

    public async Task<GetPlanningArtefactsResult> Handle(
        GetPlanningArtefactsQuery request,
        CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            return new GetPlanningArtefactsResult(false, []);
        }

        var allArtefacts = await _artefactRepository.GetByProjectIdAsync(request.ProjectId, cancellationToken);

        var planningArtefacts = allArtefacts
            .Where(artefact =>
                artefact.FilePath.StartsWith("output/planning/", StringComparison.OrdinalIgnoreCase) ||
                artefact.FilePath.StartsWith("output/tasks/", StringComparison.OrdinalIgnoreCase))
            .GroupBy(artefact => artefact.FilePath, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(artefact => artefact.Version).First())
            .OrderBy(artefact => artefact.FilePath, StringComparer.OrdinalIgnoreCase)
            .Select(artefact => new PlanningArtefactSummary(artefact.Id, artefact.FilePath, artefact.Version, artefact.CreatedAt))
            .ToList();

        return new GetPlanningArtefactsResult(true, planningArtefacts);
    }
}
