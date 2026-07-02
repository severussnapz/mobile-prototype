using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Queries.GetPrototypeDemoHtml;

public class GetPrototypeDemoHtmlQueryHandler
    : IRequestHandler<GetPrototypeDemoHtmlQuery, GetPrototypeDemoHtmlResult>
{
    private const string FilePath = "prototype-demo/index.html";

    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;

    public GetPrototypeDemoHtmlQueryHandler(
        IArtefactRepository artefactRepository,
        IArtefactStorageService artefactStorageService)
    {
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _artefactStorageService = artefactStorageService ?? throw new ArgumentNullException(nameof(artefactStorageService));
    }

    public async Task<GetPrototypeDemoHtmlResult> Handle(
        GetPrototypeDemoHtmlQuery request,
        CancellationToken cancellationToken)
    {
        var artefact = await _artefactRepository.GetByProjectAndFilePathAsync(
            request.ProjectId, FilePath, cancellationToken);
        if (artefact is null)
        {
            return GetPrototypeDemoHtmlResult.NotFound();
        }

        var html = await _artefactStorageService.GetContentAsync(artefact.S3Key, cancellationToken);
        if (html is null)
        {
            return GetPrototypeDemoHtmlResult.NotFound();
        }

        return GetPrototypeDemoHtmlResult.Succeeded(html);
    }
}
