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

    /// <summary>
    /// Returns true if the file exists (HTTP 200), false if not found (HTTP 404).
    /// Throws <see cref="System.Net.Http.HttpRequestException"/> for any other status code.
    /// </summary>
    Task<bool> FileExistsAsync(
        string installationToken,
        string owner,
        string repo,
        string path,
        CancellationToken ct);

    /// <summary>
    /// Returns the SHA of an existing file, or null if not found (HTTP 404).
    /// Throws <see cref="System.Net.Http.HttpRequestException"/> for any other status code.
    /// </summary>
    Task<string?> GetFileShaAsync(
        string installationToken,
        string owner,
        string repo,
        string path,
        CancellationToken ct);

    /// <summary>
    /// Returns true if the repository exists (HTTP 200).
    /// Returns false if not found (HTTP 404).
    /// Throws <see cref="System.Net.Http.HttpRequestException"/> for 403/401 or other errors.
    /// </summary>
    Task<bool> RepoExistsAsync(
        string token,
        string owner,
        string repo,
        CancellationToken cancellationToken);
}