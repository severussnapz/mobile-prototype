namespace Genesis.AI.Domain.Interfaces;

public interface IGitHubArtefactPushService
{
    Task PushAsync(
        Guid projectId, Guid artefactId, string filePath, int version,
        string contentType, string s3Key, string publishedByErn,
        CancellationToken ct);
}
