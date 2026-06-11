using System.Net;
using System.Text.Json;
using Genesis.AI.ApiTests.Setup;

namespace Genesis.AI.ApiTests.Tests;

public class ConversationsApiTests(GenesisAiFixture fixture) : GenesisAiBaseTest(fixture)
{
    #region Helpers

    private async Task<(Guid ProjectId, Guid StageId)> CreateProjectAndGetFirstStageAsync()
    {
        var createBody = new
        {
            code = GenerateProjectCode("CV"),
            name = $"Conversation Test {DateTime.UtcNow:HHmmss}",
            description = "Created for conversation API tests",
            timeSheetCode = "PORTASK0001045",
            complianceDomain = "Generic"
        };
        var createResponse = await Msvc.Api.CreateProjectAsync(ValidToken, createBody);
        var content = await ReadContentAsync(createResponse);
        using var doc = JsonDocument.Parse(content);
        var data = doc.RootElement.GetProperty("data");
        var projectId = data.GetProperty("id").GetGuid();
        var firstStage = data.GetProperty("pipelineStages").EnumerateArray().First();
        var stageId = firstStage.GetProperty("id").GetGuid();
        return (projectId, stageId);
    }

    private async Task<Guid> CreateConversationAsync(Guid stageId)
    {
        var body = new { stageId };
        var response = await Msvc.Api.CreateConversationAsync(ValidToken, body);
        var content = await ReadContentAsync(response);
        using var doc = JsonDocument.Parse(content);
        return doc.RootElement.GetProperty("data").GetProperty("id").GetGuid();
    }

    #endregion

    #region POST /api/v1/conversations

    [Fact]
    public async Task CreateConversation_WithValidStage_ReturnsCreated()
    {
        var (_, stageId) = await CreateProjectAndGetFirstStageAsync();

        var response = await Msvc.Api.CreateConversationAsync(ValidToken, new { stageId });
        var content = await ReadContentAsync(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var doc = JsonDocument.Parse(content);
        var data = doc.RootElement.GetProperty("data");
        Assert.True(data.TryGetProperty("id", out _));
        Assert.Equal("active", data.GetProperty("status").GetString());
    }

    [Fact]
    public async Task CreateConversation_WithoutToken_ReturnsUnauthorized()
    {

        var response = await Msvc.Api.CreateConversationAsync("", new { stageId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region GET /api/v1/conversations/{id}

    [Fact]
    public async Task GetConversation_WithValidId_ReturnsConversation()
    {
        var (_, stageId) = await CreateProjectAndGetFirstStageAsync();
        var conversationId = await CreateConversationAsync(stageId);

        var response = await Msvc.Api.GetConversationAsync(ValidToken, conversationId);
        var content = await ReadContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(content);
        var data = doc.RootElement.GetProperty("data");
        Assert.Equal(conversationId, data.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task GetConversation_WithNonExistentId_ReturnsNotFound()
    {

        var response = await Msvc.Api.GetConversationAsync(ValidToken, Guid.NewGuid());

        AssertNotFound(response);
    }

    #endregion

    #region GET /api/v1/conversations/by-stage/{stageId}

    [Fact]
    public async Task GetConversationsByStage_WithValidStage_ReturnsOk()
    {
        var (_, stageId) = await CreateProjectAndGetFirstStageAsync();
        await CreateConversationAsync(stageId);

        var response = await Msvc.Api.GetConversationsByStageAsync(ValidToken, stageId);
        var content = await ReadContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(content);
        Assert.True(doc.RootElement.TryGetProperty("data", out var dataElement));
        Assert.Equal(JsonValueKind.Array, dataElement.ValueKind);
        Assert.True(dataElement.GetArrayLength() >= 1);
    }

    #endregion

    #region Conversation State — Progress

    [Fact]
    public async Task GetProgress_WithValidConversation_ReturnsProgress()
    {
        var (_, stageId) = await CreateProjectAndGetFirstStageAsync();
        var conversationId = await CreateConversationAsync(stageId);

        var response = await Msvc.Api.GetConversationProgressAsync(ValidToken, conversationId);
        var content = await ReadContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(content);
        Assert.True(doc.RootElement.TryGetProperty("currentPhase", out _));
        Assert.True(doc.RootElement.TryGetProperty("totalPhases", out _));
    }

    [Fact]
    public async Task GetProgress_WithNonExistentId_ReturnsNotFound()
    {

        var response = await Msvc.Api.GetConversationProgressAsync(ValidToken, Guid.NewGuid());

        AssertNotFound(response);
    }

    #endregion

    #region Conversation State — Parking Lot

    [Fact]
    public async Task AddParkingLotItem_WithValidData_ReturnsCreated()
    {
        var (_, stageId) = await CreateProjectAndGetFirstStageAsync();
        var conversationId = await CreateConversationAsync(stageId);
        var body = new
        {
            content = "Deferred topic from API test",
            priority = "medium",
            sourcePhase = "Phase 1"
        };

        var response = await Msvc.Api.AddParkingLotItemAsync(ValidToken, conversationId, body);

        Assert.True(
            response.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK,
            $"Expected Created or OK but got {response.StatusCode}");
    }

    [Fact]
    public async Task GetConversationParkingLot_WithValidConversation_ReturnsOk()
    {
        var (_, stageId) = await CreateProjectAndGetFirstStageAsync();
        var conversationId = await CreateConversationAsync(stageId);

        var response = await Msvc.Api.GetConversationParkingLotAsync(ValidToken, conversationId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion
}
