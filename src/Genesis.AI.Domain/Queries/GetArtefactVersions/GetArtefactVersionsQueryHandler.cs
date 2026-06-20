using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Queries.GetArtefactVersions;

public class GetArtefactVersionsQueryHandler : IRequestHandler<GetArtefactVersionsQuery, GetArtefactVersionsResult>
{
    private readonly IArtefactRepository _artefactRepository;

    public GetArtefactVersionsQueryHandler(IArtefactRepository artefactRepository)
    {
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
    }

    public async Task<GetArtefactVersionsResult> Handle(GetArtefactVersionsQuery request, CancellationToken cancellationToken)
    {
        var versions = await _artefactRepository.GetVersionsByFilePathAsync(
            request.ProjectId,
            request.FilePath,
            cancellationToken);

        return new GetArtefactVersionsResult(versions);
    }
}
