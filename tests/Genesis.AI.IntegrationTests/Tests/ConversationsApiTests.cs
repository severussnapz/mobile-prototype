using System.Net;
using System.Text.Json;

namespace Genesis.AI.IntegrationTests.Tests;

public class ConversationsApiTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public ConversationsApiTests()
    {
        _factory = new TestWebApplicationFactory();
    }

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private static async Task<(string ProjectId, string StageId)> CreateProjectAndGetFirstStageAsync(HttpClient client)
    {
        var content = new StringContent(
            """{"code":"CONV","name":"Conv Test","description":"Test","timeSheetCode":"PORTASK0001045","complianceDomain":"Generic"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var response = await client.PostAsync("/api/v1/projects", content);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var data = doc.RootElement.GetProperty("data");
        var projectId = data.GetProperty("id").GetString()!;
        var firstStage = data.GetProperty("pipelineStages").EnumerateArray().First();
        var stageId = firstStage.GetProperty("id").GetString()!;
        return (projectId, stageId);
    }

    [Fact]
    public async Task CreateConversation_WithValidStageId_Returns201Created()
    {
        var client = _factory.CreateAdminClient();
        var (_, stageId) = await CreateProjectAndGetFirstStageAsync(client);

        var content = new StringContent(
            $$$"""{"stageId":"{{{stageId}}}"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/v1/conversations", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("data", out var data));
        Assert.True(data.TryGetProperty("id", out _));
    }

    [Fact]
    public async Task GetConversation_WithValidId_ReturnsConversation()
    {
        var client = _factory.CreateAdminClient();
        var (_, stageId) = await CreateProjectAndGetFirstStageAsync(client);

        // Create conversation
        var createContent = new StringContent(
            $$$"""{"stageId":"{{{stageId}}}"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var createResponse = await client.PostAsync("/api/v1/conversations", createContent);
        var createBody = await createResponse.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createBody);
        var conversationId = createDoc.RootElement.GetProperty("data").GetProperty("id").GetString();

        // Get it
        var response = await client.GetAsync($"/api/v1/conversations/{conversationId}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("data", out _));
    }

    [Fact]
    public async Task GetConversation_WithNonExistentId_Returns404NotFound()
    {
        var client = _factory.CreateAdminClient();

        var response = await client.GetAsync($"/api/v1/conversations/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetConversationsByStage_WithValidStageId_ReturnsConversations()
    {
        var client = _factory.CreateAdminClient();
        var (_, stageId) = await CreateProjectAndGetFirstStageAsync(client);

        // Create a conversation
        var createContent = new StringContent(
            $$$"""{"stageId":"{{{stageId}}}"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        await client.PostAsync("/api/v1/conversations", createContent);

        // List by stage
        var response = await client.GetAsync($"/api/v1/conversations/by-stage/{stageId}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        var data = doc.RootElement.GetProperty("data");
        Assert.True(data.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task CreateConversation_WithoutToken_Returns401Unauthorised()
    {
        var client = _factory.CreateClient();
        var content = new StringContent(
            $$$"""{"stageId":"{{{Guid.NewGuid()}}}"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/v1/conversations", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
