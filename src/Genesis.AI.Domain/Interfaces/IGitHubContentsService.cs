using Genesis.AI.Domain.GitHub;

namespace Genesis.AI.Domain.Interfaces;

public interface IGitHubContentsService
{
    Task<GitHubPushResult> PushFileAsync(
        string installationToken,
        string owner,
        string repo,
        string path,
        byte[] content,
        string commitMessage,
        string? existingSha,
        CancellationToken ct);
}