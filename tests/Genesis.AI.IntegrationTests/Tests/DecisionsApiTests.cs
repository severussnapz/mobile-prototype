using System.Net;
using System.Text.Json;

namespace Genesis.AI.IntegrationTests.Tests;

public class DecisionsApiTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public DecisionsApiTests()
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
            """{"code":"DEC","name":"Decision Test","description":"Test","timeSheetCode":"PORTASK0001045","complianceDomain":"Generic"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var response = await client.PostAsync("/api/v1/projects", content);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("data").GetProperty("id").GetString()!;
    }

    private static async Task<string> CreateDecisionAsync(HttpClient client, string projectId)
    {
        var payload = new StringContent(
            """{"title":"Use PostgreSQL","context":"Need a relational store","decision":"Adopt PostgreSQL 17","consequences":"Operational familiarity"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var response = await client.PostAsync($"/api/v1/projects/{projectId}/decisions", payload);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("data").GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task GetDecisions_ForNewProject_ReturnsEmptyArray()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);

        var response = await client.GetAsync($"/api/v1/projects/{projectId}/decisions");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(0, doc.RootElement.GetProperty("data").GetArrayLength());
    }

    [Fact]
    public async Task GetDecisions_ForUnknownProject_Returns404NotFound()
    {
        var client = _factory.CreateAdminClient();

        var response = await client.GetAsync($"/api/v1/projects/{Guid.NewGuid()}/decisions");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateDecision_WithValidPayload_ReturnsCreated()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);

        var payload = new StringContent(
            """{"title":"Use PostgreSQL","context":"Need a relational store","decision":"Adopt PostgreSQL 17","consequences":"Operational familiarity"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync($"/api/v1/projects/{projectId}/decisions", payload);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("Use PostgreSQL", doc.RootElement.GetProperty("data").GetProperty("title").GetString());
    }

    [Fact]
    public async Task CreateDecision_ForUnknownProject_Returns404NotFound()
    {
        var client = _factory.CreateAdminClient();
        var payload = new StringContent(
            """{"title":"Orphan","context":"c","decision":"d","consequences":"x"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync($"/api/v1/projects/{Guid.NewGuid()}/decisions", payload);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateDecision_WithValidPayload_ReturnsUpdatedTitle()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);
        var decisionId = await CreateDecisionAsync(client, projectId);

        var payload = new StringContent(
            """{"title":"Use PostgreSQL 17","context":"Updated","decision":"Adopt","consequences":"Updated"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PatchAsync(
            $"/api/v1/projects/{projectId}/decisions/{decisionId}", payload);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("Use PostgreSQL 17", doc.RootElement.GetProperty("data").GetProperty("title").GetString());
    }

    [Fact]
    public async Task UpdateDecision_ForUnknownDecision_Returns404NotFound()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);

        var payload = new StringContent(
            """{"title":"t","context":"c","decision":"d","consequences":"x"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PatchAsync(
            $"/api/v1/projects/{projectId}/decisions/{Guid.NewGuid()}", payload);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDecision_WithValidId_Returns204NoContent()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);
        var decisionId = await CreateDecisionAsync(client, projectId);

        var response = await client.DeleteAsync(
            $"/api/v1/projects/{projectId}/decisions/{decisionId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDecision_ForUnknownDecision_Returns404NotFound()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);

        var response = await client.DeleteAsync(
            $"/api/v1/projects/{projectId}/decisions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetDecisions_WithoutToken_Returns401Unauthorised()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/projects/{Guid.NewGuid()}/decisions");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
