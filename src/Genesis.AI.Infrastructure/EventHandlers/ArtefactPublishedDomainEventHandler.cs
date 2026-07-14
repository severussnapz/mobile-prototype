using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Genesis.AI.Infrastructure.EventHandlers;

/// <summary>
/// Indexes published artefact content into the project knowledge namespace (pgvector)
/// and pushes the artefact to GitHub when an <see cref="ArtefactPublishedDomainEvent"/>
/// is dispatched.
///
/// Domain events fire from <c>DatabaseContext.SaveChangesAsync</c> BEFORE
/// <c>base.SaveChangesAsync</c>. The artefact content is written to object storage before
/// that save is called, so <see cref="IArtefactStorageService.GetContentAsync"/> is safe here.
///
/// Both side effects are best-effort: any failure is logged and swallowed so it can never
/// fail the artefact save that triggered it. Indexing and push are independent — a failure
/// in one must not prevent the other.
/// </summary>
[SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "MediatR notification handlers are named '<Event>Handler' by convention; the 'EventHandler' suffix denotes an INotificationHandler, not a CLR event delegate.")]
public sealed class ArtefactPublishedDomainEventHandler
    : INotificationHandler<ArtefactPublishedDomainEvent>
{
    // Only text-bearing formats produce useful vectors. text/html is tag soup (low signal);
    // xlsx/docx are binary. Both are skipped silently.
    private static readonly HashSet<string> IndexableContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "text/markdown",
            "text/plain"
        };

    private readonly IArtefactStorageService _storageService;
    private readonly IKnowledgeService _knowledgeService;
    private readonly IGitHubArtefactPushService _pushService;
    private readonly ILogger<ArtefactPublishedDomainEventHandler> _logger;

    public ArtefactPublishedDomainEventHandler(
        IArtefactStorageService storageService,
        IKnowledgeService knowledgeService,
        IGitHubArtefactPushService pushService,
        ILogger<ArtefactPublishedDomainEventHandler> logger)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _knowledgeService = knowledgeService ?? throw new ArgumentNullException(nameof(knowledgeService));
        _pushService = pushService ?? throw new ArgumentNullException(nameof(pushService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(
        ArtefactPublishedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        // Indexing — only for text-bearing content types.
        if (IndexableContentTypes.Contains(notification.ContentType))
        {
            try
            {
                var content = await _storageService.GetContentAsync(notification.S3Key, cancellationToken);
                if (string.IsNullOrWhiteSpace(content))
                {
                    _logger.LogWarning(
                        "ArtefactPublishedDomainEventHandler: no content at {S3Key} for {FilePath} — skipping indexing",
                        notification.S3Key, notification.FilePath);
                }
                else
                {
                    var metadata = new Dictionary<string, string>
                    {
                        ["contentType"] = notification.ContentType,
                        ["filePath"] = notification.FilePath
                    };

                    // sourcePath is the version-independent file path so re-publishing a new version
                    // overwrites the same knowledge document rather than accumulating stale vectors.
                    await _knowledgeService.IndexDocumentAsync(
                        KnowledgeNamespace.ProjectArtefact,
                        notification.ProjectId,
                        notification.FilePath,
                        content,
                        metadata,
                        cancellationToken);

                    _logger.LogInformation(
                        "ArtefactPublishedDomainEventHandler: indexed {FilePath} for project {ProjectId}",
                        notification.FilePath, notification.ProjectId);
                }
            }
            catch (Exception exception)
            {
                // Best-effort — indexing must never fail the artefact save that raised this event.
                _logger.LogError(
                    exception,
                    "ArtefactPublishedDomainEventHandler: failed to index {FilePath} for project {ProjectId}",
                    notification.FilePath, notification.ProjectId);
            }
        }

        // Push — independent of content type and indexing result.
        try
        {
            await _pushService.PushAsync(
                notification.ProjectId,
                notification.ArtefactId,
                notification.FilePath,
                notification.Version,
                notification.ContentType,
                notification.S3Key,
                notification.TriggeredBy,
                cancellationToken);
        }
        catch (Exception exception)
        {
            // Best-effort — push failures must never fail the artefact save that raised this event.
            _logger.LogError(
                exception,
                "ArtefactPublishedDomainEventHandler: failed to push {FilePath} to GitHub",
                notification.FilePath);
        }
    }
}
