using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Genesis.AI.Domain.Exceptions;
using Genesis.AI.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Genesis.AI.Infrastructure.Services.GitHub;

public sealed class GitHubAppTokenService : IGitHubTokenService
{
    private static readonly TimeSpan CacheSafetyMargin = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly TimeProvider _timeProvider;
    private readonly string _appId;
    private readonly RSA _rsa;
    private readonly ConcurrentDictionary<string, (string Token, DateTimeOffset ExpiresAt)> _tokenCache = new();

    public GitHubAppTokenService(HttpClient httpClient, TimeProvider timeProvider)
        : this(
            httpClient,
            timeProvider,
            Environment.GetEnvironmentVariable("GITHUB_APP_ID")
                ?? throw new InvalidOperationException("Environment variable 'GITHUB_APP_ID' was not found."),
            Environment.GetEnvironmentVariable("GITHUB_APP_PRIVATE_KEY")
                ?? throw new InvalidOperationException("Environment variable 'GITHUB_APP_PRIVATE_KEY' was not found."))
    {
    }

    private GitHubAppTokenService(HttpClient httpClient, TimeProvider timeProvider, string appId, string privateKeyPem)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _appId = appId;
        _rsa = RSA.Create();
        _rsa.ImportFromPem(privateKeyPem.Replace("\\n", "\n", StringComparison.Ordinal));
    }

    public async Task<string> GetInstallationTokenAsync(string installationId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installationId);

        var utcNow = _timeProvider.GetUtcNow();
        if (TryGetCachedToken(installationId, utcNow, out var cachedToken))
        {
            return cachedToken;
        }

        var jwt = CreateJwt(utcNow);
        var accessTokenUri = $"https://api.github.com/app/installations/{installationId}/access_tokens";

        using var request = new HttpRequestMessage(HttpMethod.Post, accessTokenUri)
        {
            Headers =
            {
                Authorization = new AuthenticationHeaderValue("Bearer", jwt),
                Accept = { new MediaTypeWithQualityHeaderValue("application/vnd.github+json") }
            }
        };
        request.Headers.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
        request.Headers.UserAgent.ParseAdd("genesis-ai");

        try
        {
            using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new GitHubAuthenticationException("GitHub App installation token request was rejected with HTTP 401.");
            }

            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var (token, expiresAt) = ParseTokenResponse(payload);

            _tokenCache[installationId] = (token, expiresAt);
            return token;
        }
        catch (GitHubAuthenticationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new GitHubAuthenticationException("Failed to obtain a GitHub installation token.", exception);
        }
    }

    private bool TryGetCachedToken(string installationId, DateTimeOffset utcNow, out string token)
    {
        if (_tokenCache.TryGetValue(installationId, out var cached)
            && cached.ExpiresAt - utcNow > CacheSafetyMargin)
        {
            token = cached.Token;
            return true;
        }

        token = string.Empty;
        return false;
    }

    private static (string Token, DateTimeOffset ExpiresAt) ParseTokenResponse(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        var token = document.RootElement.GetProperty("token").GetString()
            ?? throw new GitHubAuthenticationException("GitHub installation token response did not contain a token.");
        var expiresAt = document.RootElement.GetProperty("expires_at").GetDateTimeOffset();
        return (token, expiresAt);
    }

    private string CreateJwt(DateTimeOffset utcNow)
    {
        var now = utcNow.UtcDateTime;
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = _appId,
            NotBefore = now.AddSeconds(-60),
            IssuedAt = now.AddSeconds(-60),
            Expires = now.AddMinutes(10),
            SigningCredentials = new SigningCredentials(
                new RsaSecurityKey(_rsa),
                SecurityAlgorithms.RsaSha256)
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.CreateEncodedJwt(tokenDescriptor);
    }
}