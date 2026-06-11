using System.Net;
using System.Text.Json;
using Genesis.AI.ApiTests.Setup;

namespace Genesis.AI.ApiTests.Tests;

public class DecisionsApiTests(GenesisAiFixture fixture) : GenesisAiBaseTest(fixture)
{
    #region Helpers

    private async Task<Guid> CreateProjectAsync()
    {
        var body = new
        {
            code = GenerateProjectCode("DEC"),
            name = $"Decisions Test {DateTime.UtcNow:HHmmss}",
            description = "Created for decisions API tests",
            timeSheetCode = "PORTASK0001045",
            complianceDomain = "Generic"
        };
        var response = await Msvc.Api.CreateProjectAsync(ValidToken, body);
        var content = await ReadContentAsync(response);
        using var doc = JsonDocument.Parse(content);
        return doc.RootElement.GetProperty("data").GetProperty("id").GetGuid();
    }

    private static object DecisionPayload(string title) => new
    {
        title,
        context = "The context for the decision.",
        decision = "The decision that was made.",
        consequences = "The consequences of the decision."
    };

    private async Task<Guid> CreateDecisionAsync(Guid projectId, string title)
    {
        var response = await Msvc.Api.CreateDecisionAsync(ValidToken, projectId, DecisionPayload(title));
        var content = await ReadContentAsync(response);
        using var doc = JsonDocument.Parse(content);
        return doc.RootElement.GetProperty("data").GetProperty("id").GetGuid();
    }

    #endregion

    #region GET /api/v1/projects/{projectId}/decisions

    [Fact]
    public async Task GetDecisions_ForNewProject_ReturnsEmptyArray()
    {
        var projectId = await CreateProjectAsync();

        var response = await Msvc.Api.GetDecisionsAsync(ValidToken, projectId);
        var content = await ReadContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(content);
        var data = doc.RootElement.GetProperty("data");
        Assert.Equal(JsonValueKind.Array, data.ValueKind);
        Assert.Equal(0, data.GetArrayLength());
    }

    [Fact]
    public async Task GetDecisions_ForUnknownProject_ReturnsNotFound()
    {
        var response = await Msvc.Api.GetDecisionsAsync(ValidToken, Guid.NewGuid());

        AssertNotFound(response);
    }

    [Fact]
    public async Task GetDecisions_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Msvc.Api.GetDecisionsAsync("", Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region POST /api/v1/projects/{projectId}/decisions

    [Fact]
    public async Task CreateDecision_WithValidPayload_ReturnsCreated()
    {
        var projectId = await CreateProjectAsync();

        var response = await Msvc.Api.CreateDecisionAsync(
            ValidToken, projectId, DecisionPayload("Use PostgreSQL native enums"));
        var content = await ReadContentAsync(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var doc = JsonDocument.Parse(content);
        var data = doc.RootElement.GetProperty("data");
        Assert.True(data.TryGetProperty("id", out _));
        Assert.Equal("Use PostgreSQL native enums", data.GetProperty("title").GetString());
    }

    [Fact]
    public async Task CreateDecision_ForUnknownProject_ReturnsNotFound()
    {
        var response = await Msvc.Api.CreateDecisionAsync(
            ValidToken, Guid.NewGuid(), DecisionPayload("Orphan decision"));

        AssertNotFound(response);
    }

    [Fact]
    public async Task CreateDecision_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Msvc.Api.CreateDecisionAsync("", Guid.NewGuid(), DecisionPayload("No token"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region PATCH /api/v1/projects/{projectId}/decisions/{decisionId}

    [Fact]
    public async Task UpdateDecision_WithValidPayload_ReturnsUpdatedTitle()
    {
        var projectId = await CreateProjectAsync();
        var decisionId = await CreateDecisionAsync(projectId, "Original title");

        var response = await Msvc.Api.UpdateDecisionAsync(
            ValidToken, projectId, decisionId, DecisionPayload("Revised title"));
        var content = await ReadContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(content);
        Assert.Equal("Revised title", doc.RootElement.GetProperty("data").GetProperty("title").GetString());
    }

    [Fact]
    public async Task UpdateDecision_ForUnknownDecision_ReturnsNotFound()
    {
        var projectId = await CreateProjectAsync();

        var response = await Msvc.Api.UpdateDecisionAsync(
            ValidToken, projectId, Guid.NewGuid(), DecisionPayload("Does not exist"));

        AssertNotFound(response);
    }

    #endregion

    #region DELETE /api/v1/projects/{projectId}/decisions/{decisionId}

    [Fact]
    public async Task DeleteDecision_WithValidId_ReturnsNoContent()
    {
        var projectId = await CreateProjectAsync();
        var decisionId = await CreateDecisionAsync(projectId, "Decision to delete");

        var response = await Msvc.Api.DeleteDecisionAsync(ValidToken, projectId, decisionId);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDecision_ForUnknownDecision_ReturnsNotFound()
    {
        var projectId = await CreateProjectAsync();

        var response = await Msvc.Api.DeleteDecisionAsync(ValidToken, projectId, Guid.NewGuid());

        AssertNotFound(response);
    }

    #endregion
}
