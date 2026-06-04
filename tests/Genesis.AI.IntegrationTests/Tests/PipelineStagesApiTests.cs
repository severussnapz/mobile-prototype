using System.Net;
using System.Text.Json;

namespace Genesis.AI.IntegrationTests.Tests;

public class PipelineStagesApiTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public PipelineStagesApiTests()
    {
        _factory = new TestWebApplicationFactory();
    }

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private static async Task<string> GetFirstStageIdAsync(HttpClient client)
    {
        var content = new StringContent(
            """{"code":"STG","name":"Stage Test","description":"Test","timeSheetCode":"PORTASK0001045","complianceDomain":"Generic"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var response = await client.PostAsync("/api/v1/projects", content);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("data")
            .GetProperty("pipelineStages")
            .EnumerateArray()
            .First()
            .GetProperty("id")
            .GetString()!;
    }

    private static async Task<string> GetFirstStageIdWithConversationAsync(HttpClient client)
    {
        // Create project
        var content = new StringContent(
            """{"code":"STG2","name":"Stage Test","description":"Test","timeSheetCode":"PORTASK0001045","complianceDomain":"Generic"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var response = await client.PostAsync("/api/v1/projects", content);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var data = doc.RootElement.GetProperty("data");
        var projectId = data.GetProperty("id").GetString()!;
        var stageId = data.GetProperty("pipelineStages")
            .EnumerateArray()
            .First()
            .GetProperty("id")
            .GetString()!;

        // Create a conversation to move stage to InProgress
        var convContent = new StringContent(
            $"{{\"stageId\":\"{stageId}\"}}",
            System.Text.Encoding.UTF8,
            "application/json");
        await client.PostAsync("/api/v1/conversations", convContent);

        // Create an artefact (required for completion)
        var artefactContent = new StringContent(
            """{"artefacts":[{"filePath":"docs/manifest.md","contentType":"text/markdown","content":"# Test"}]}""",
            System.Text.Encoding.UTF8,
            "application/json");
        await client.PostAsync($"/api/v1/projects/{projectId}/artefacts", artefactContent);

        return stageId;
    }

    [Fact]
    public async Task CompleteStage_WithValidId_ReturnsOk()
    {
        var client = _factory.CreateAdminClient();
        var stageId = await GetFirstStageIdWithConversationAsync(client);

        var response = await client.PostAsync($"/api/v1/stages/{stageId}/complete", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CompleteStage_WithNonExistentId_Returns404NotFound()
    {
        var client = _factory.CreateAdminClient();

        var response = await client.PostAsync($"/api/v1/stages/{Guid.NewGuid()}/complete", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SkipStage_WithValidId_ReturnsOk()
    {
        var client = _factory.CreateAdminClient();
        var stageId = await GetFirstStageIdAsync(client);

        var response = await client.PostAsync($"/api/v1/stages/{stageId}/skip", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CompleteStage_WithoutToken_Returns401Unauthorised()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"/api/v1/stages/{Guid.NewGuid()}/complete", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CompleteStage_WithReadOnlyToken_Returns403Forbidden()
    {
        var client = _factory.CreateReadOnlyClient();

        var response = await client.PostAsync($"/api/v1/stages/{Guid.NewGuid()}/complete", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
