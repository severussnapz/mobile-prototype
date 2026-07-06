using System.Runtime.CompilerServices;
using Genesis.AI.Domain.AggregatesModel.ArtefactAggregate;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Genesis.AI.Infrastructure.Interceptors;

/// <summary>
/// Detects published artefacts (created or promoted to published, or amended while published)
/// and indexes their content into the Genesis AI Knowledge Service post-commit.
/// 
/// Uses ConditionalWeakTable to hand off captured artefacts between SavingChangesAsync
/// (pre-commit capture) and SavedChangesAsync (post-commit indexing). This prevents
/// data races on the singleton interceptor when multiple requests save concurrently.
/// </summary>
public sealed class ArtefactPublishedInterceptor : ISaveChangesInterceptor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ArtefactPublishedInterceptor> _logger;
    
    // State hand-off between SavingChangesAsync and SavedChangesAsync,
    // keyed by DbContext to prevent cross-request races on the singleton interceptor.
    private readonly ConditionalWeakTable<DbContext, List<ArtefactIndexRequest>> _pending
        = new();

    private static readonly HashSet<string> IndexableContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text/markdown",
        "text/plain"
        // Note: text/html excluded (tag soup — low-signal vectors per design decision)
        // Note: application/vnd.openxmlformats* excluded (binary — not indexable)
    };

    public ArtefactPublishedInterceptor(
        IServiceScopeFactory scopeFactory,
        ILogger<ArtefactPublishedInterceptor> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null)
        {
            return new ValueTask<InterceptionResult<int>>(result);
        }

        // Guard: do not process under InMemory provider (integration tests)
        if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            return new ValueTask<InterceptionResult<int>>(result);
        }

        var pendingList = new List<ArtefactIndexRequest>();

        // Detect all artefacts eligible for indexing
        foreach (var entry in context.ChangeTracker.Entries<Artefact>())
        {
            var artefact = entry.Entity;

            // Case 1: Born-published (Added, IsPublished = true)
            if (entry.State == EntityState.Added
                && artefact.IsPublished
                && IndexableContentTypes.Contains(artefact.ContentType))
            {
                pendingList.Add(new ArtefactIndexRequest(
                    artefact.ProjectId,
                    artefact.FilePath,
                    artefact.S3Key,
                    artefact.ContentType));
                continue;
            }

            // Case 2: Promoted to published (Modified, false → true)
            if (entry.State == EntityState.Modified)
            {
                var isPublishedProperty = entry.Property(nameof(Artefact.IsPublished));
                if (isPublishedProperty.OriginalValue is false
                    && artefact.IsPublished
                    && IndexableContentTypes.Contains(artefact.ContentType))
                {
                    pendingList.Add(new ArtefactIndexRequest(
                        artefact.ProjectId,
                        artefact.FilePath,
                        artefact.S3Key,
                        artefact.ContentType));
                    continue;
                }

                // Case 3: Amendment to already-published (Modified, S3Key or Version changed)
                if (artefact.IsPublished
                    && (entry.Property(nameof(Artefact.S3Key)).IsModified
                        || entry.Property(nameof(Artefact.Version)).IsModified)
                    && IndexableContentTypes.Contains(artefact.ContentType))
                {
                    pendingList.Add(new ArtefactIndexRequest(
                        artefact.ProjectId,
                        artefact.FilePath,
                        artefact.S3Key,
                        artefact.ContentType));
                }
            }
        }

        // Store the pending list keyed by this context
        // (will be retrieved in SavedChangesAsync after commit)
        if (pendingList.Count > 0)
        {
            _pending.Add(context, pendingList);
        }

        return new ValueTask<InterceptionResult<int>>(result);
    }

    public async ValueTask<InterceptionResult<int>> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null)
        {
            return result;
        }

        // Retrieve and remove the pending list for this context
        if (!_pending.TryGetValue(context, out var pendingList) || pendingList.Count == 0)
        {
            return result;
        }

        _pending.Remove(context);

        // Index all pending artefacts asynchronously
        // Run indexing in a new scope to resolve scoped services
        using var scope = _scopeFactory.CreateScope();
        var knowledgeService = scope.ServiceProvider.GetRequiredService<IKnowledgeService>();
        var storageService = scope.ServiceProvider.GetRequiredService<IArtefactStorageService>();

        // Index with best-effort error handling — never throw
        await IndexPendingArtefactsAsync(pendingList, knowledgeService, storageService, cancellationToken);

        return result;
    }

    public ValueTask<InterceptionResult> SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context != null)
        {
            // Clean up the pending list on failure — transaction is rolling back
            _pending.Remove(context);
        }

        return new ValueTask<InterceptionResult>(result);
    }

    private async Task IndexPendingArtefactsAsync(
        List<ArtefactIndexRequest> pendingList,
        IKnowledgeService knowledgeService,
        IArtefactStorageService storageService,
        CancellationToken cancellationToken)
    {
        foreach (var request in pendingList)
        {
            try
            {
                // Fetch artefact content from S3
                var content = await storageService.GetContentAsync(request.S3Key, cancellationToken);
                if (string.IsNullOrWhiteSpace(content))
                {
                    _logger.LogWarning(
                        "Artefact {FilePath} (project {ProjectId}) has empty content in S3 ({S3Key}); skipping indexing.",
                        request.FilePath, request.ProjectId, request.S3Key);
                    continue;
                }

                // Index into knowledge service (sourcePath = FilePath, version-independent)
                await knowledgeService.IndexDocumentAsync(
                    KnowledgeNamespace.ProjectArtefact,
                    request.ProjectId,
                    request.FilePath,  // Version-independent key for delete-then-insert atomicity
                    content,
                    new Dictionary<string, string>
                    {
                        ["contentType"] = request.ContentType,
                        ["filePath"] = request.FilePath
                    },
                    cancellationToken);
            }
            catch (Exception ex)
            {
                // Best-effort — approval already committed, never throw from SavedChangesAsync
                // Log and continue to next artefact
                _logger.LogError(ex,
                    "Knowledge indexing failed for artefact {FilePath} in project {ProjectId}.",
                    request.FilePath, request.ProjectId);
            }
        }
    }
}
