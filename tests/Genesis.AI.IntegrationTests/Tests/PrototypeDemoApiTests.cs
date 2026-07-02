using System.Net;
using System.Text;
using System.Text.Json;

namespace Genesis.AI.IntegrationTests.Tests;

// Day 0 harness: end-to-end smoke test for the Plan-4 prototype-demo endpoint.
// These compile now (no new types referenced) and fail at runtime until the
// PrototypeDemoController route exists — routing returns 404 today, so the
// 200/401 assertions are genuinely red (no false greens).
public class PrototypeDemoApiTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public PrototypeDemoApiTests()
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

    // Smoke test: auth + routing only. Content correctness is covered by the
    // service- and handler-level unit tests, not re-asserted over HTTP here.
    [Fact]
    public async Task GeneratePrototypeDemo_WithWriteScope_ReturnsOkWithHtmlContentType()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);

        var response = await client.PostAsync(
            $"/api/v1/projects/{projectId}/prototype-demo", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GeneratePrototypeDemo_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync(
            $"/api/v1/projects/{Guid.NewGuid()}/prototype-demo", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
