using System.Net;
using System.Text;
using System.Text.Json;
using Genesis.AI.Domain.Exceptions;
using Genesis.AI.Domain.GitHub;
using Genesis.AI.Infrastructure.Services.GitHub;

namespace Genesis.AI.Tests.Infrastructure.Services.GitHub;

public sealed class GitHubContentsServiceTests
{
    [Fact]
    public async Task PushFileAsync_ContentExceeds12MB_ThrowsGitHubFileTooLargeException_BeforeAnyHttpCall()
    {
        var handlerInvoked = false;
        var handler = new StubHttpMessageHandler(request =>
        {
            handlerInvoked = true;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        var service = CreateService(handler);
        var oversized = new byte[(12 * 1024 * 1024) + 1];

        await Assert.ThrowsAsync<GitHubFileTooLargeException>(() =>
            service.PushFileAsync(
                "token",
                "owner",
                "repo",
                "test.md",
                oversized,
                "feat: push",
                null,
                CancellationToken.None));

            Assert.False(handlerInvoked);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task PushFileAsync_WithExistingSha_IncludesShaInPutBody()
    {
        var handler = new StubHttpMessageHandler(_ =>
            Task.FromResult(CreatePutSuccessResponse()));
        var service = CreateService(handler);

        _ = await service.PushFileAsync(
            "token",
            "owner",
            "repo",
            "test.md",
            Encoding.UTF8.GetBytes("content"),
            "feat: push",
            "abc123",
            CancellationToken.None);

        var putBody = ParseJson(handler.RequestBodies.Single());
        Assert.True(putBody.TryGetProperty("sha", out var sha));
        Assert.Equal("abc123", sha.GetString());
    }

    [Fact]
    public async Task PushFileAsync_NullExistingSha_OmitsShaFromPutBody()
    {
        var handler = new StubHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(
                    "{\"commit\":{\"sha\":\"deadbeef\"},\"content\":{\"html_url\":\"https://github.com/owner/repo/blob/main/test.md\"}}",
                    Encoding.UTF8,
                    "application/json")
            }));
        var service = CreateService(handler);

        _ = await service.PushFileAsync(
            "token",
            "owner",
            "repo",
            "test.md",
            Encoding.UTF8.GetBytes("content"),
            "feat: push",
            null,
            CancellationToken.None);

        var putBody = ParseJson(handler.RequestBodies.Single());
        var hasSha = putBody.TryGetProperty("sha", out var sha);
        Assert.True(!hasSha || sha.ValueKind == JsonValueKind.Null);
    }

    [Fact]
    public async Task PushFileAsync_ContentBase64EncodedInPutBody()
    {
        var content = Encoding.UTF8.GetBytes("test content");
        var handler = new StubHttpMessageHandler(_ =>
            Task.FromResult(CreatePutSuccessResponse()));
        var service = CreateService(handler);

        _ = await service.PushFileAsync(
            "token",
            "owner",
            "repo",
            "test.md",
            content,
            "feat: push",
            null,
            CancellationToken.None);

        var putBody = ParseJson(handler.RequestBodies.Single());
        Assert.Equal(Convert.ToBase64String(content), putBody.GetProperty("content").GetString());
    }

    [Fact]
    public async Task PushFileAsync_SuccessfulPush_ReturnsCommitShaAndFileUrl()
    {
        var handler = new StubHttpMessageHandler(_ =>
            Task.FromResult(CreatePutSuccessResponse()));
        var service = CreateService(handler);

        GitHubPushResult result = await service.PushFileAsync(
            "token",
            "owner",
            "repo",
            "test.md",
            Encoding.UTF8.GetBytes("content"),
            "feat: push",
            null,
            CancellationToken.None);

        Assert.Equal("deadbeef", result.CommitSha);
        Assert.Equal("https://github.com/owner/repo/blob/main/test.md", result.FileUrl);
    }

    [Fact]
    public async Task PushFileAsync_Returns422_RefetchesShaAndRetries()
    {
        var putCount = 0;
        var getCount = 0;

        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.Method == HttpMethod.Put)
            {
                putCount++;

                if (putCount == 1)
                {
                    return Task.FromResult(new HttpResponseMessage((HttpStatusCode)422)
                    {
                        Content = new StringContent("{\"message\":\"sha wasn't supplied\"}", Encoding.UTF8, "application/json")
                    });
                }

                return Task.FromResult(CreatePutSuccessResponse());
            }

            if (request.Method == HttpMethod.Get)
            {
                getCount++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"sha\":\"newsha456\"}", Encoding.UTF8, "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
        });

        var service = CreateService(handler);

        _ = await service.PushFileAsync(
            "token",
            "owner",
            "repo",
            "test.md",
            Encoding.UTF8.GetBytes("content"),
            "feat: push",
            null,
            CancellationToken.None);

        Assert.Equal(2, putCount);
        Assert.Equal(1, getCount);

        var finalPutBody = ParseJson(handler.RequestBodies.Last());
        Assert.Equal("newsha456", finalPutBody.GetProperty("sha").GetString());
    }

    [Fact]
    public async Task FileExistsAsync_Returns200_ReturnsTrue()
    {
        var handler = new StubHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)));
        var service = CreateService(handler);

        var result = await service.FileExistsAsync(
            "token",
            "owner",
            "repo",
            ".genesis/.gitkeep",
            CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task FileExistsAsync_Returns404_ReturnsFalse()
    {
        var handler = new StubHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound)));
        var service = CreateService(handler);

        var result = await service.FileExistsAsync(
            "token",
            "owner",
            "repo",
            ".genesis/.gitkeep",
            CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task FileExistsAsync_Returns500_Throws()
    {
        var handler = new StubHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)));
        var service = CreateService(handler);

        await Assert.ThrowsAsync<System.Net.Http.HttpRequestException>(() =>
            service.FileExistsAsync(
                "token",
                "owner",
                "repo",
                ".genesis/.gitkeep",
                CancellationToken.None));
    }

    private static GitHubContentsService CreateService(StubHttpMessageHandler handler)
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.github.com/")
        };

        return new GitHubContentsService(client);
    }

    private static HttpResponseMessage CreatePutSuccessResponse()
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "{\"commit\":{\"sha\":\"deadbeef\"},\"content\":{\"html_url\":\"https://github.com/owner/repo/blob/main/test.md\"}}",
                Encoding.UTF8,
                "application/json")
        };
    }

    private static JsonElement ParseJson(string json)
    {
        return JsonDocument.Parse(json).RootElement;
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        public int RequestCount { get; private set; }

        public List<string> RequestBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;

            if (request.Content is not null)
            {
                var body = await request.Content.ReadAsStringAsync(cancellationToken);
                RequestBodies.Add(body);
            }

            return await _handler(request);
        }
    }
}
