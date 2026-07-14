using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Time.Testing;
using Genesis.AI.Domain.Exceptions;
using Genesis.AI.Infrastructure.Services.GitHub;

namespace Genesis.AI.Tests.Infrastructure.Services.GitHub;

public sealed class GitHubAppTokenServiceTests : IDisposable
{
    private const string AppId = "4238033";
    private const string InstallationId = "144995615";

    private readonly string? _originalAppId;
    private readonly string? _originalPrivateKey;
    private readonly RSA _rsa;

    public GitHubAppTokenServiceTests()
    {
        _originalAppId = Environment.GetEnvironmentVariable("GITHUB_APP_ID");
        _originalPrivateKey = Environment.GetEnvironmentVariable("GITHUB_APP_PRIVATE_KEY");

        _rsa = RSA.Create(2048);
        Environment.SetEnvironmentVariable("GITHUB_APP_ID", AppId);
        Environment.SetEnvironmentVariable("GITHUB_APP_PRIVATE_KEY", _rsa.ExportRSAPrivateKeyPem());
    }

    [Fact]
    public async Task GetInstallationTokenAsync_MintsJwtWithCorrectClaims()
    {
        var handlerInvoked = false;
        var handler = new StubHttpMessageHandler(async request =>
        {
            handlerInvoked = true;
            await Task.CompletedTask;

            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal($"/app/installations/{InstallationId}/access_tokens", request.RequestUri?.AbsolutePath);

            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(
                    "{\"token\":\"inst-token\",\"expires_at\":\"2030-01-01T00:00:00Z\"}",
                    Encoding.UTF8,
                    "application/json")
            };
        });

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.github.com/")
        };
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 07, 07, 10, 0, 0, TimeSpan.Zero));
        var service = new GitHubAppTokenService(client, timeProvider);

        _ = await service.GetInstallationTokenAsync(InstallationId, CancellationToken.None);

        Assert.True(handlerInvoked);
        Assert.NotNull(handler.LastRequest);
        Assert.NotNull(handler.LastRequest!.Headers.Authorization);
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization!.Scheme);

        var jwt = handler.LastRequest.Headers.Authorization.Parameter;
        Assert.False(string.IsNullOrWhiteSpace(jwt));

        var token = new JwtSecurityTokenHandler().ReadJwtToken(jwt!);
        Assert.Equal(AppId, token.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Iss).Value);

        var iat = long.Parse(token.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Iat).Value);
        var exp = long.Parse(token.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Exp).Value);

        Assert.Equal(660, exp - iat);
        Assert.True(iat <= DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task GetInstallationTokenAsync_CachesToken_DoesNotCallApiTwice()
    {
        var callCount = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            callCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(
                    "{\"token\":\"cached-token\",\"expires_at\":\"2030-01-01T00:00:00Z\"}",
                    Encoding.UTF8,
                    "application/json")
            });
        });

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.github.com/")
        };
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 07, 07, 10, 0, 0, TimeSpan.Zero));
        var service = new GitHubAppTokenService(client, timeProvider);

        var first = await service.GetInstallationTokenAsync(InstallationId, CancellationToken.None);
        var second = await service.GetInstallationTokenAsync(InstallationId, CancellationToken.None);

        Assert.Equal("cached-token", first);
        Assert.Equal(first, second);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task GetInstallationTokenAsync_ExpiredCache_RemintesToken()
    {
        var callCount = 0;
        var now = new DateTimeOffset(2026, 07, 07, 10, 0, 0, TimeSpan.Zero);
        var firstExpiry = now.AddMinutes(4).ToUniversalTime().ToString("O");
        var secondExpiry = now.AddHours(1).ToUniversalTime().ToString("O");

        var handler = new StubHttpMessageHandler(_ =>
        {
            callCount++;
            var payload = callCount == 1
                ? $"{{\"token\":\"near-expiry\",\"expires_at\":\"{firstExpiry}\"}}"
                : $"{{\"token\":\"reminted\",\"expires_at\":\"{secondExpiry}\"}}";

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            });
        });

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.github.com/")
        };
        var timeProvider = new FakeTimeProvider(now);
        var service = new GitHubAppTokenService(client, timeProvider);

        var first = await service.GetInstallationTokenAsync(InstallationId, CancellationToken.None);
        var second = await service.GetInstallationTokenAsync(InstallationId, CancellationToken.None);

        Assert.Equal("near-expiry", first);
        Assert.Equal("reminted", second);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task GetInstallationTokenAsync_ApiReturns401_ThrowsGitHubAuthenticationException()
    {
        var handler = new StubHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.github.com/")
        };
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 07, 07, 10, 0, 0, TimeSpan.Zero));
        var service = new GitHubAppTokenService(client, timeProvider);

        await Assert.ThrowsAsync<GitHubAuthenticationException>(() =>
            service.GetInstallationTokenAsync(InstallationId, CancellationToken.None));
    }

    public void Dispose()
    {
        _rsa.Dispose();
        Environment.SetEnvironmentVariable("GITHUB_APP_ID", _originalAppId);
        Environment.SetEnvironmentVariable("GITHUB_APP_PRIVATE_KEY", _originalPrivateKey);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return await _handler(request);
        }
    }
}
