using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Genesis.AI.Domain.Exceptions;
using Genesis.AI.Domain.GitHub;
using Genesis.AI.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Genesis.AI.Infrastructure.Services.GitHub;

public sealed class GitHubContentsService : IGitHubContentsService
{
    private const int MaxContentBytes = 12 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubContentsService> _logger;

    public GitHubContentsService(HttpClient httpClient, ILogger<GitHubContentsService>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? NullLogger<GitHubContentsService>.Instance;
    }

    public async Task<GitHubPushResult> PushFileAsync(
        string installationToken,
        string owner,
        string repo,
        string path,
        byte[] content,
        string commitMessage,
        string? existingSha,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installationToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(commitMessage);
        ArgumentNullException.ThrowIfNull(content);

        if (content.Length > MaxContentBytes)
        {
            throw new GitHubFileTooLargeException("GitHub Contents API cannot accept files larger than 12 MB.");
        }

        var resolvedSha = existingSha;
        var fileUrl = $"https://api.github.com/repos/{owner}/{repo}/contents/{path}";
        var requestUri = new Uri(fileUrl);

        using var firstResponse = await SendPutAsync(
            requestUri,
            installationToken,
            path,
            content,
            commitMessage,
            resolvedSha,
            ct).ConfigureAwait(false);

        if (firstResponse.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
        {
            resolvedSha = await GetCurrentShaAsync(requestUri, installationToken, ct).ConfigureAwait(false);
            using var retryResponse = await SendPutAsync(
                requestUri,
                installationToken,
                path,
                content,
                commitMessage,
                resolvedSha,
                ct).ConfigureAwait(false);

            return await ReadPushResultAsync(retryResponse, ct).ConfigureAwait(false);
        }

        return await ReadPushResultAsync(firstResponse, ct).ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> SendPutAsync(
        Uri requestUri,
        string installationToken,
        string path,
        byte[] content,
        string commitMessage,
        string? sha,
        CancellationToken ct)
    {
        var body = new Dictionary<string, object?>
        {
            ["message"] = commitMessage,
            ["content"] = Convert.ToBase64String(content)
        };

        if (!string.IsNullOrWhiteSpace(sha))
        {
            body["sha"] = sha;
        }

        using var request = new HttpRequestMessage(HttpMethod.Put, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", installationToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
        request.Headers.UserAgent.ParseAdd("genesis-ai");
        request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");

        return await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
    }

    public async Task<bool> FileExistsAsync(
        string installationToken,
        string owner,
        string repo,
        string path,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installationToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var requestUri = new Uri($"https://api.github.com/repos/{owner}/{repo}/contents/{path}");
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", installationToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
        request.Headers.UserAgent.ParseAdd("genesis-ai");

        using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    private async Task<string> GetCurrentShaAsync(Uri requestUri, string installationToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", installationToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
        request.Headers.UserAgent.ParseAdd("genesis-ai");

        using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var document = JsonDocument.Parse(payload);
        return document.RootElement.GetProperty("sha").GetString()
            ?? throw new GitHubAuthenticationException("GitHub file lookup response did not contain a sha.");
    }

    private static async Task<GitHubPushResult> ReadPushResultAsync(HttpResponseMessage response, CancellationToken ct)
    {
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var document = JsonDocument.Parse(payload);
        var commitSha = document.RootElement.GetProperty("commit").GetProperty("sha").GetString()
            ?? throw new GitHubAuthenticationException("GitHub push response did not contain a commit sha.");
        var fileUrl = document.RootElement.GetProperty("content").GetProperty("html_url").GetString()
            ?? throw new GitHubAuthenticationException("GitHub push response did not contain a file url.");

        return new GitHubPushResult(commitSha, fileUrl);
    }

    public async Task<string?> GetFileShaAsync(
        string installationToken,
        string owner,
        string repo,
        string path,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installationToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var requestUri = new Uri($"https://api.github.com/repos/{owner}/{repo}/contents/{path}");
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", installationToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
        request.Headers.UserAgent.ParseAdd("genesis-ai");

        using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var document = JsonDocument.Parse(payload);
        return document.RootElement.GetProperty("sha").GetString();
    }
}