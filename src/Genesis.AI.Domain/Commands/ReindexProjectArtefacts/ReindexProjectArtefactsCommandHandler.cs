using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Genesis.AI.Domain.Commands.ReindexProjectArtefacts;

public sealed class ReindexProjectArtefactsCommandHandler
    : IRequestHandler<ReindexProjectArtefactsCommand, ReindexProjectArtefactsResult>
{
    private static readonly HashSet<string> IndexableContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "text/markdown",
            "text/plain"
        };

    private readonly IArtefactRepository _artefactRepository;
    private readonly IArtefactStorageService _artefactStorageService;
    private readonly IKnowledgeService _knowledgeService;
    private readonly ILogger<ReindexProjectArtefactsCommandHandler> _logger;

    public ReindexProjectArtefactsCommandHandler(
        IArtefactRepository artefactRepository,
        IArtefactStorageService artefactStorageService,
        IKnowledgeService knowledgeService,
        ILogger<ReindexProjectArtefactsCommandHandler> logger)
    {
        _artefactRepository = artefactRepository ?? throw new ArgumentNullException(nameof(artefactRepository));
        _artefactStorageService = artefactStorageService ?? throw new ArgumentNullException(nameof(artefactStorageService));
        _knowledgeService = knowledgeService ?? throw new ArgumentNullException(nameof(knowledgeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReindexProjectArtefactsResult> Handle(
        ReindexProjectArtefactsCommand request,
        CancellationToken cancellationToken)
    {
        var manifest = await _artefactRepository.GetProjectArtefactManifestAsync(request.ProjectId, cancellationToken);
        var publishedTextArtefacts = manifest
            .Where(artefact => artefact.IsPublished)
            .Where(artefact => IndexableContentTypes.Contains(artefact.ContentType))
            .ToList();

        var indexed = 0;
        var skipped = 0;
        var failed = 0;

        foreach (var artefact in publishedTextArtefacts)
        {
            try
            {
                var content = await _artefactStorageService.GetContentAsync(artefact.S3Key, cancellationToken);
                if (string.IsNullOrWhiteSpace(content))
                {
                    skipped++;
                    continue;
                }

                var metadata = new Dictionary<string, string>
                {
                    ["contentType"] = artefact.ContentType,
                    ["filePath"] = artefact.FilePath
                };

                await _knowledgeService.IndexDocumentAsync(
                    KnowledgeNamespace.ProjectArtefact,
                    request.ProjectId,
                    artefact.FilePath,
                    content,
                    metadata,
                    cancellationToken);

                indexed++;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to reindex artefact {FilePath} for project {ProjectId}",
                    artefact.FilePath,
                    request.ProjectId);
                failed++;
            }
        }

        return new ReindexProjectArtefactsResult(indexed, skipped, failed);
    }
}
