using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Domain.Normalisation;
using MediatR;

namespace Genesis.AI.Domain.Queries.GetNormalisationArtefacts;

public sealed class GetNormalisationArtefactsQueryHandler
    : IRequestHandler<GetNormalisationArtefactsQuery, GetNormalisationArtefactsResult>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IArtefactRepository _artefactRepository;

    public GetNormalisationArtefactsQueryHandler(
        IProjectRepository projectRepository,
        IArtefactRepository artefactRepository)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
    }

    public async Task<GetNormalisationArtefactsResult> Handle(
        GetNormalisationArtefactsQuery request,
        CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            return new GetNormalisationArtefactsResult(false, []);
        }

        var artefacts = await _artefactRepository.GetByProjectIdAsync(request.ProjectId, cancellationToken);

        var outputArtefacts = artefacts
            .Where(artefact => artefact.FilePath.StartsWith("output/", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(artefact => artefact.CreatedAt)
            .Select(artefact => new NormalisationArtefactSummary(
                artefact.Id,
                artefact.FilePath,
                artefact.Version,
                artefact.CreatedAt))
            .ToList();

        return new GetNormalisationArtefactsResult(true, outputArtefacts);
    }
}
