using Genesis.AI.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Genesis.AI.Infrastructure.Services.GitHub;

internal sealed class NoOpGitHubArtefactPushService : IGitHubArtefactPushService
{
    private readonly ILogger<NoOpGitHubArtefactPushService> _logger;

    public NoOpGitHubArtefactPushService(ILogger<NoOpGitHubArtefactPushService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task PushAsync(
        Guid projectId,
        Guid artefactId,
        string filePath,
        int version,
        string contentType,
        string s3Key,
        string triggeredBy,
        CancellationToken ct)
    {
        _logger.LogWarning(
            "GitHub is not configured; skipping push for artefact {ArtefactId} at {FilePath}",
            artefactId,
            filePath);

        return Task.CompletedTask;
    }
}
