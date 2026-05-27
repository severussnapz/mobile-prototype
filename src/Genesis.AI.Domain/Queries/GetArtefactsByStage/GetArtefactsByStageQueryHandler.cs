using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Queries.GetArtefactsByStage;

public class GetArtefactsByStageQueryHandler : IRequestHandler<GetArtefactsByStageQuery, IReadOnlyList<Artefact>>
{
    private readonly IArtefactRepository _artefactRepository;

    public GetArtefactsByStageQueryHandler(IArtefactRepository artefactRepository)
    {
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
    }

    public async Task<IReadOnlyList<Artefact>> Handle(GetArtefactsByStageQuery request, CancellationToken cancellationToken)
    {
        return await _artefactRepository.GetByProjectIdAsync(request.ProjectId, cancellationToken);
    }
}
