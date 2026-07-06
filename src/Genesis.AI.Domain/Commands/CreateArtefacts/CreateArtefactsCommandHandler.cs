using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.CreateArtefacts;

public class CreateArtefactsCommandHandler : IRequestHandler<CreateArtefactsCommand, IReadOnlyList<Artefact>>
{
    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;
    private readonly TimeProvider _timeProvider;

    public CreateArtefactsCommandHandler(
        IArtefactRepository artefactRepository,
        IArtefactStorageService artefactStorageService,
        TimeProvider timeProvider)
    {
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _artefactStorageService = artefactStorageService ?? throw new ArgumentNullException(nameof(artefactStorageService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<IReadOnlyList<Artefact>> Handle(CreateArtefactsCommand request, CancellationToken cancellationToken)
    {
        var results = new List<Artefact>();

        foreach (var item in request.Artefacts)
        {
            if (string.IsNullOrWhiteSpace(item.FilePath) || string.IsNullOrWhiteSpace(item.Content))
                continue;

            var nextVersion = await _artefactRepository.GetNextVersionAsync(request.ProjectId, cancellationToken);
            var contentType = item.ContentType ?? "text/markdown";
            var filePath = item.FilePath.Trim();

            var storageKey = await _artefactStorageService.SaveContentAsync(
                request.ProjectId,
                filePath,
                nextVersion,
                item.Content,
                contentType,
                cancellationToken);

            var artefact = Artefact.CreateS3Artefact(
                request.ProjectId,
                nextVersion,
                filePath,
                storageKey,
                contentType,
                System.Text.Encoding.UTF8.GetByteCount(item.Content),
                request.UserId, _timeProvider, true);

            await _artefactRepository.AddAsync(artefact, cancellationToken);
            results.Add(artefact);
        }

        await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return results;
    }
}
