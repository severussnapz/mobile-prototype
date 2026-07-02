using System.Net;
using System.Text;
using System.Text.Json;
using Genesis.AI.Domain.Interfaces;
using Moq;

namespace Genesis.AI.IntegrationTests.Tests;

// Plan-4 SSE harness: tests for the streaming prototype-demo endpoint
// (POST /api/v1/projects/{projectId}/prototype-demo/stream).
//
// These compile now (no new types referenced) and are RED until the streaming
// route exists — routing returns 404 today, so every assertion below genuinely
// fails for the right reason (missing endpoint), not a false green.
//
// The contract asserted here is the LOCKED contract and holds regardless of the
// CSS-injection-timing decision (stream assembled output vs stream raw chunks +
// assemble in the done event):
//   - Content-Type is text/event-stream
//   - a `started` status event is emitted first
//   - at least one `chunk` event precedes the terminal `done` event
//   - the `done` event data carries the COMPLETE assembled HTML document
//   - an AI failure surfaces as an `error` event (no partial 200 body swallowed)
//   - auth mirrors the synchronous endpoint (ProjectWrite scope required)
public class PrototypeDemoStreamApiTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public PrototypeDemoStreamApiTests()
    {
        _factory = new TestWebApplicationFactory();
    }

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private static async Task<string> CreateProjectAsync(HttpClient client)
    {
        var content = new StringContent(
            """{"code":"PROTO","name":"Prototype Test","description":"Test","timeSheetCode":"TS-001","complianceDomain":"Generic"}""",
            Encoding.UTF8,
            "application/json");
        var response = await client.PostAsync("/api/v1/projects", content);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("data").GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task StreamPrototypeDemo_WithWriteScope_ReturnsEventStreamContentType()
    {
        _factory.AiServiceMock
            .Setup(service => service.StreamResponseAsync(
                It.IsAny<AiSystemPrompt>(),
                It.IsAny<IReadOnlyList<AiMessage>>(),
                It.IsAny<CancellationToken>()))
            .Returns(MinimalHtmlStream());

        var client = _factory.CreateWriteClient();
        var projectId = await CreateProjectAsync(client);

        var response = await client.PostAsync(
            $"/api/v1/projects/{projectId}/prototype-demo/stream", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task StreamPrototypeDemo_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync(
            $"/api/v1/projects/{Guid.NewGuid()}/prototype-demo/stream", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task StreamPrototypeDemo_WithWriteScope_EmitsStartedThenChunkThenDoneInOrder()
    {
        _factory.AiServiceMock
            .Setup(service => service.StreamResponseAsync(
                It.IsAny<AiSystemPrompt>(),
                It.IsAny<IReadOnlyList<AiMessage>>(),
                It.IsAny<CancellationToken>()))
            .Returns(MinimalHtmlStream());

        var client = _factory.CreateWriteClient();
        var projectId = await CreateProjectAsync(client);

        var response = await client.PostAsync(
            $"/api/v1/projects/{projectId}/prototype-demo/stream", content: null);
        var events = ParseSse(await response.Content.ReadAsStringAsync());

        var firstStatus = events.FindIndex(e => e.Event == "status");
        var firstChunk = events.FindIndex(e => e.Event == "chunk");
        var doneIndex = events.FindIndex(e => e.Event == "done");

        Assert.True(firstStatus >= 0, "expected a status event");
        Assert.True(firstChunk >= 0, "expected at least one chunk event");
        Assert.True(doneIndex >= 0, "expected a terminal done event");
        Assert.True(firstStatus < firstChunk, "status must precede the first chunk");
        Assert.True(firstChunk < doneIndex, "at least one chunk must precede done");
    }

    [Fact]
    public async Task StreamPrototypeDemo_DoneEvent_CarriesCompleteAssembledHtml()
    {
        _factory.AiServiceMock
            .Setup(service => service.StreamResponseAsync(
                It.IsAny<AiSystemPrompt>(),
                It.IsAny<IReadOnlyList<AiMessage>>(),
                It.IsAny<CancellationToken>()))
            .Returns(MinimalHtmlStream());

        var client = _factory.CreateWriteClient();
        var projectId = await CreateProjectAsync(client);

        var response = await client.PostAsync(
            $"/api/v1/projects/{projectId}/prototype-demo/stream", content: null);
        var events = ParseSse(await response.Content.ReadAsStringAsync());

        var done = events.Single(e => e.Event == "done");
        using var doc = JsonDocument.Parse(done.Data);
        var html = doc.RootElement.GetProperty("html").GetString();

        Assert.NotNull(html);
        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("</html>", html);
        Assert.Contains("</head>", html);
        Assert.Contains("PROTOTYPE ONLY", html);
    }

    [Fact]
    public async Task StreamPrototypeDemo_WhenGenerationFails_EmitsErrorEvent()
    {
        _factory.AiServiceMock
            .Setup(service => service.StreamResponseAsync(
                It.IsAny<AiSystemPrompt>(),
                It.IsAny<IReadOnlyList<AiMessage>>(),
                It.IsAny<CancellationToken>()))
            .Returns(ThrowingStream());

        var client = _factory.CreateWriteClient();
        var projectId = await CreateProjectAsync(client);

        var response = await client.PostAsync(
            $"/api/v1/projects/{projectId}/prototype-demo/stream", content: null);
        var events = ParseSse(await response.Content.ReadAsStringAsync());

        var error = events.Single(e => e.Event == "error");
        using var doc = JsonDocument.Parse(error.Data);
        Assert.True(doc.RootElement.TryGetProperty("code", out _), "error event must carry a code");
        Assert.True(doc.RootElement.TryGetProperty("message", out _), "error event must carry a message");
        Assert.DoesNotContain(events, e => e.Event == "done");
    }

    private static List<(string Event, string Data)> ParseSse(string body)
    {
        var events = new List<(string Event, string Data)>();
        foreach (var block in body.Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            string eventName = "message";
            var data = new StringBuilder();
            foreach (var line in block.Split('\n'))
            {
                if (line.StartsWith("event:", StringComparison.Ordinal))
                {
                    eventName = line["event:".Length..].Trim();
                }
                else if (line.StartsWith("data:", StringComparison.Ordinal))
                {
                    data.Append(line["data:".Length..].TrimStart());
                }
            }

            events.Add((eventName, data.ToString()));
        }

        return events;
    }

    private static async IAsyncEnumerable<string> MinimalHtmlStream()
    {
        await Task.CompletedTask;
        yield return "<!DOCTYPE html><html lang=\"en\"><head></head>";
        yield return "<body>PROTOTYPE ONLY</body></html>";
    }

    private static async IAsyncEnumerable<string> ThrowingStream()
    {
        await Task.CompletedTask;
        if (DateTime.UtcNow.Year > 0)
        {
            throw new InvalidOperationException("bedrock boom");
        }

        yield break;
    }
}
