using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Genesis.AI.IntegrationTests.Tests;

// End-to-end coverage for prototype-demo persistence: a generated demo is saved as
// a project artefact under prototype-demo/index.html and restored on reload. These
// exercise the full HTTP → MediatR → repository → in-memory storage round-trip.
public class PrototypeDemoPersistenceApiTests : IDisposable
{
    private const string SampleHtml =
        "<!DOCTYPE html><html lang=\"en\"><head></head><body>SAVED DEMO</body></html>";
    private const string UpdatedHtml =
        "<!DOCTYPE html><html lang=\"en\"><head></head><body>UPDATED DEMO</body></html>";

    private readonly TestWebApplicationFactory _factory;

    public PrototypeDemoPersistenceApiTests()
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
            """{"code":"PROTOSAVE","name":"Prototype Save Test","description":"Test","timeSheetCode":"TS-001","complianceDomain":"Generic"}""",
            Encoding.UTF8,
            "application/json");
        var response = await client.PostAsync("/api/v1/projects", content);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("data").GetProperty("id").GetString()!;
    }

    private static StringContent HtmlBody(string html)
    {
        return new StringContent(html, Encoding.UTF8, "text/html");
    }

    [Fact]
    public async Task SaveThenFetch_ReturnsIdenticalHtmlWithHtmlContentType()
    {
        var client = _factory.CreateWriteClient();
        var projectId = await CreateProjectAsync(client);

        await client.PostAsync(
            $"/api/v1/projects/{projectId}/prototype-demo/save", HtmlBody(SampleHtml));

        var fetchResponse = await client.GetAsync(
            $"/api/v1/projects/{projectId}/prototype-demo/html");

        Assert.Equal(HttpStatusCode.OK, fetchResponse.StatusCode);
        Assert.Equal("text/html", fetchResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal(SampleHtml, await fetchResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task SaveTwice_KeepsSingleArtefactId()
    {
        var client = _factory.CreateWriteClient();
        var projectId = await CreateProjectAsync(client);

        var firstSave = await client.PostAsync(
            $"/api/v1/projects/{projectId}/prototype-demo/save", HtmlBody(SampleHtml));
        var secondSave = await client.PostAsync(
            $"/api/v1/projects/{projectId}/prototype-demo/save", HtmlBody(UpdatedHtml));

        var firstArtefactId = await firstSave.Content.ReadAsStringAsync();
        var secondArtefactId = await secondSave.Content.ReadAsStringAsync();

        // Same row (in-place version bump), so the artefact ID is stable across saves.
        Assert.Equal(firstArtefactId, secondArtefactId);
    }

    [Fact]
    public async Task SaveTwice_FetchReturnsLatestHtml()
    {
        var client = _factory.CreateWriteClient();
        var projectId = await CreateProjectAsync(client);

        await client.PostAsync(
            $"/api/v1/projects/{projectId}/prototype-demo/save", HtmlBody(SampleHtml));
        await client.PostAsync(
            $"/api/v1/projects/{projectId}/prototype-demo/save", HtmlBody(UpdatedHtml));

        var fetchResponse = await client.GetAsync(
            $"/api/v1/projects/{projectId}/prototype-demo/html");
        Assert.Equal(UpdatedHtml, await fetchResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Fetch_BeforeAnySave_ReturnsNotFound()
    {
        var client = _factory.CreateWriteClient();
        var projectId = await CreateProjectAsync(client);

        var response = await client.GetAsync(
            $"/api/v1/projects/{projectId}/prototype-demo/html");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Save_AgainstMissingProject_ReturnsNotFound()
    {
        var client = _factory.CreateWriteClient();

        var response = await client.PostAsync(
            $"/api/v1/projects/{Guid.NewGuid()}/prototype-demo/save", HtmlBody(SampleHtml));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Save_WithoutWriteScope_ReturnsForbidden()
    {
        var writeClient = _factory.CreateWriteClient();
        var projectId = await CreateProjectAsync(writeClient);

        var readClient = _factory.CreateReadOnlyClient();
        var response = await readClient.PostAsync(
            $"/api/v1/projects/{projectId}/prototype-demo/save", HtmlBody(SampleHtml));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Save_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync(
            $"/api/v1/projects/{Guid.NewGuid()}/prototype-demo/save", HtmlBody(SampleHtml));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Fetch_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var response = await client.GetAsync(
            $"/api/v1/projects/{Guid.NewGuid()}/prototype-demo/html");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
