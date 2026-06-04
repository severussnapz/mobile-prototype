using System.Net;
using System.Text.Json;

namespace Genesis.AI.IntegrationTests.Tests;

public class ArtefactsApiTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public ArtefactsApiTests()
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
            """{"code":"ART","name":"Artefact Test","description":"Test","timeSheetCode":"PORTASK0001045","complianceDomain":"Generic"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var response = await client.PostAsync("/api/v1/projects", content);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("data").GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task GetArtefacts_ForNewProject_ReturnsEmptyArray()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);

        var response = await client.GetAsync($"/api/v1/projects/{projectId}/artefacts");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(0, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task CreateArtefacts_WithValidPayload_ReturnsCreated()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);

        var payload = new StringContent(
            """{"artefacts":[{"filePath":"docs/manifest.md","contentType":"text/markdown","content":"# Manifest\n\nTest artefact."}]}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync($"/api/v1/projects/{projectId}/artefacts", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetArtefacts_AfterCreation_ReturnsArtefacts()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);
        var payload = new StringContent(
            """{"artefacts":[{"filePath":"docs/test.md","contentType":"text/markdown","content":"# Test"}]}""",
            System.Text.Encoding.UTF8,
            "application/json");
        await client.PostAsync($"/api/v1/projects/{projectId}/artefacts", payload);

        var response = await client.GetAsync($"/api/v1/projects/{projectId}/artefacts");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task GetArtefacts_WithoutToken_Returns401Unauthorised()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/projects/{Guid.NewGuid()}/artefacts");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
