using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Queries.GetArtefactById;

public class GetArtefactByIdQueryHandler : IRequestHandler<GetArtefactByIdQuery, Artefact?>
{
    private readonly IArtefactRepository _artefactRepository;

    public GetArtefactByIdQueryHandler(IArtefactRepository artefactRepository)
    {
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
    }

    public async Task<Artefact?> Handle(GetArtefactByIdQuery request, CancellationToken cancellationToken)
    {
        return await _artefactRepository.GetByIdAsync(request.ArtefactId, cancellationToken);
    }
}
