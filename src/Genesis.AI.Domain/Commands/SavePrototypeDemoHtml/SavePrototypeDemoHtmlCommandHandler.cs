using System.Text;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Commands.SavePrototypeDemoHtml;

/// <summary>
/// Handles <see cref="SavePrototypeDemoHtmlCommand"/>: persists the prototype-demo HTML
/// as a versioned text artefact under <c>prototype-demo/index.html</c>. Mirrors the
/// regenerate-in-place pattern used by the hazard-log export — a single artefact row
/// whose version climbs on each save. This path is distinct from
/// <c>prototype/index.html</c>, which is owned by the fragment assembly pipeline.
/// </summary>
public class SavePrototypeDemoHtmlCommandHandler
    : IRequestHandler<SavePrototypeDemoHtmlCommand, SavePrototypeDemoHtmlResult>
{
    private const string FilePath = "prototype-demo/index.html";
    private const string ContentType = "text/html";

    private readonly IProjectRepository _projectRepository;
    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;
    private readonly TimeProvider _timeProvider;

    public SavePrototypeDemoHtmlCommandHandler(
        IProjectRepository projectRepository,
        IArtefactRepository artefactRepository,
        IArtefactStorageService artefactStorageService,
        TimeProvider timeProvider)
    {
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _artefactStorageService = artefactStorageService ?? throw new ArgumentNullException(nameof(artefactStorageService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SavePrototypeDemoHtmlResult> Handle(
        SavePrototypeDemoHtmlCommand request,
        CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            return SavePrototypeDemoHtmlResult.Failure(
                SavePrototypeDemoHtmlStatus.ProjectNotFound,
                $"No project found with ID '{request.ProjectId}'.");
        }

        var sizeBytes = Encoding.UTF8.GetByteCount(request.Html);

        var existing = await _artefactRepository.GetByProjectAndFilePathAsync(
            request.ProjectId, FilePath, cancellationToken);

        if (existing is not null)
        {
            var nextVersion = existing.Version + 1;
            var existingStorageKey = await _artefactStorageService.SaveContentAsync(
                request.ProjectId, FilePath, nextVersion, request.Html, ContentType, cancellationToken);

            var tracked = await _artefactRepository.GetByIdAsync(existing.Id, cancellationToken);
            tracked!.ReplaceContent(
                nextVersion,
                existingStorageKey,
                ContentType,
                sizeBytes,
                request.UserId,
                _timeProvider);

            await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return SavePrototypeDemoHtmlResult.Succeeded(tracked.Id);
        }

        var storageKey = await _artefactStorageService.SaveContentAsync(
            request.ProjectId, FilePath, 1, request.Html, ContentType, cancellationToken);

        var artefact = Artefact.CreateS3Artefact(
            request.ProjectId,
            1,
            FilePath,
            storageKey,
            ContentType,
            sizeBytes,
            request.UserId, _timeProvider, true);

        await _artefactRepository.AddAsync(artefact, cancellationToken);
        await _artefactRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return SavePrototypeDemoHtmlResult.Succeeded(artefact.Id);
    }
}
