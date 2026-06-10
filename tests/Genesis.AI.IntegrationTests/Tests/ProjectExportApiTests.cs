using System.Net;
using System.Text.Json;

namespace Genesis.AI.IntegrationTests.Tests;

public class ProjectExportApiTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public ProjectExportApiTests()
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
            """{"code":"EXPORT","name":"Export Test","description":"Test","timeSheetCode":"PORTASK0001045","complianceDomain":"Generic"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var response = await client.PostAsync("/api/v1/projects", content);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("data").GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task ExportProject_ForUnknownProject_Returns404NotFound()
    {
        var client = _factory.CreateAdminClient();

        var response = await client.GetAsync($"/api/v1/projects/{Guid.NewGuid()}/export");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ExportProject_ForNewProject_ReturnsZipFile()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);

        var response = await client.GetAsync($"/api/v1/projects/{projectId}/export");
        var bytes = await response.Content.ReadAsByteArrayAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/zip", response.Content.Headers.ContentType?.MediaType);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public async Task ExportProject_WithArtefacts_ReturnsZipContainingFiles()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);
        var payload = new StringContent(
            """{"artefacts":[{"filePath":"docs/manifest.md","contentType":"text/markdown","content":"# Manifest"}]}""",
            System.Text.Encoding.UTF8,
            "application/json");
        await client.PostAsync($"/api/v1/projects/{projectId}/artefacts", payload);

        var response = await client.GetAsync($"/api/v1/projects/{projectId}/export");
        var bytes = await response.Content.ReadAsByteArrayAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var archive = new System.IO.Compression.ZipArchive(
            new MemoryStream(bytes), System.IO.Compression.ZipArchiveMode.Read);
        Assert.Contains(archive.Entries, entry => entry.FullName == "README.md");
        Assert.Contains(archive.Entries, entry => entry.FullName.Contains("manifest.md", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExportProject_WithoutToken_Returns401Unauthorised()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/projects/{Guid.NewGuid()}/export");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
