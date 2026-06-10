using System.Net;
using System.Text.Json;

namespace Genesis.AI.IntegrationTests.Tests;

public class NotesApiTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public NotesApiTests()
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
            """{"code":"NOTE","name":"Note Test","description":"Test","timeSheetCode":"PORTASK0001045","complianceDomain":"Generic"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var response = await client.PostAsync("/api/v1/projects", content);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("data").GetProperty("id").GetString()!;
    }

    private static async Task<string> CreateNoteAsync(HttpClient client, string projectId, string noteContent)
    {
        var payload = new StringContent(
            $$"""{"content":"{{noteContent}}"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var response = await client.PostAsync($"/api/v1/projects/{projectId}/notes", payload);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("data").GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task GetNotes_ForNewProject_ReturnsEmptyArray()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);

        var response = await client.GetAsync($"/api/v1/projects/{projectId}/notes");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.GetProperty("data").ValueKind);
        Assert.Equal(0, doc.RootElement.GetProperty("data").GetArrayLength());
    }

    [Fact]
    public async Task GetNotes_ForUnknownProject_Returns404NotFound()
    {
        var client = _factory.CreateAdminClient();

        var response = await client.GetAsync($"/api/v1/projects/{Guid.NewGuid()}/notes");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateNote_WithValidPayload_ReturnsCreated()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);

        var payload = new StringContent(
            """{"content":"First note"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync($"/api/v1/projects/{projectId}/notes", payload);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("First note", doc.RootElement.GetProperty("data").GetProperty("content").GetString());
    }

    [Fact]
    public async Task CreateNote_ForUnknownProject_Returns404NotFound()
    {
        var client = _factory.CreateAdminClient();
        var payload = new StringContent(
            """{"content":"Orphan note"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync($"/api/v1/projects/{Guid.NewGuid()}/notes", payload);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateNote_WithValidPayload_ReturnsUpdatedContent()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);
        var noteId = await CreateNoteAsync(client, projectId, "Original");

        var payload = new StringContent(
            """{"content":"Updated content"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PatchAsync($"/api/v1/projects/{projectId}/notes/{noteId}", payload);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("Updated content", doc.RootElement.GetProperty("data").GetProperty("content").GetString());
    }

    [Fact]
    public async Task UpdateNote_ForUnknownNote_Returns404NotFound()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);

        var payload = new StringContent(
            """{"content":"Updated"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PatchAsync(
            $"/api/v1/projects/{projectId}/notes/{Guid.NewGuid()}", payload);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteNote_WithValidId_Returns204NoContent()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);
        var noteId = await CreateNoteAsync(client, projectId, "To delete");

        var response = await client.DeleteAsync($"/api/v1/projects/{projectId}/notes/{noteId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteNote_ForUnknownNote_Returns404NotFound()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);

        var response = await client.DeleteAsync(
            $"/api/v1/projects/{projectId}/notes/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetNotes_WithoutToken_Returns401Unauthorised()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/projects/{Guid.NewGuid()}/notes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateNote_WithReadOnlyToken_Returns403Forbidden()
    {
        var adminClient = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(adminClient);
        var readOnlyClient = _factory.CreateReadOnlyClient();

        var payload = new StringContent(
            """{"content":"Should fail"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await readOnlyClient.PostAsync($"/api/v1/projects/{projectId}/notes", payload);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
