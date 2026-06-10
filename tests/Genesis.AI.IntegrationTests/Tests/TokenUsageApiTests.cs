using System.Net;
using System.Text.Json;

namespace Genesis.AI.IntegrationTests.Tests;

public class TokenUsageApiTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public TokenUsageApiTests()
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
            """{"code":"TOK","name":"Token Usage Test","description":"Test","timeSheetCode":"PORTASK0001045","complianceDomain":"Generic"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var response = await client.PostAsync("/api/v1/projects", content);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("data").GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task GetTokenUsage_ForNewProject_ReturnsZeroedTotals()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);

        var response = await client.GetAsync($"/api/v1/projects/{projectId}/token-usage");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        var totals = doc.RootElement.GetProperty("data").GetProperty("totals");
        Assert.Equal(0, totals.GetProperty("inputTokens").GetInt32());
        Assert.Equal(0, totals.GetProperty("outputTokens").GetInt32());
        Assert.Equal(0, totals.GetProperty("turnCount").GetInt32());
    }

    [Fact]
    public async Task GetTokenUsage_ForNewProject_ReturnsEmptyStages()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);

        var response = await client.GetAsync($"/api/v1/projects/{projectId}/token-usage");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        var stages = doc.RootElement.GetProperty("data").GetProperty("stages");
        Assert.Equal(JsonValueKind.Array, stages.ValueKind);
        Assert.Equal(0, stages.GetArrayLength());
    }

    [Fact]
    public async Task GetTokenUsage_WithReadOnlyToken_ReturnsOk()
    {
        var adminClient = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(adminClient);
        var readClient = _factory.CreateReadOnlyClient();

        var response = await readClient.GetAsync($"/api/v1/projects/{projectId}/token-usage");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTokenUsage_WithoutToken_Returns401Unauthorised()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/projects/{Guid.NewGuid()}/token-usage");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
