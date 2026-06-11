using System.Net;
using System.Text.Json;
using Genesis.AI.ApiTests.Setup;

namespace Genesis.AI.ApiTests.Tests;

public class TokenUsageApiTests(GenesisAiFixture fixture) : GenesisAiBaseTest(fixture)
{
    #region Helpers

    private async Task<Guid> CreateProjectAsync()
    {
        var body = new
        {
            code = GenerateProjectCode("TOK"),
            name = $"Token Usage Test {DateTime.UtcNow:HHmmss}",
            description = "Created for token usage API tests",
            timeSheetCode = "PORTASK0001045",
            complianceDomain = "Generic"
        };
        var response = await Msvc.Api.CreateProjectAsync(ValidToken, body);
        var content = await ReadContentAsync(response);
        using var doc = JsonDocument.Parse(content);
        return doc.RootElement.GetProperty("data").GetProperty("id").GetGuid();
    }

    #endregion

    #region GET /api/v1/projects/{projectId}/token-usage

    [Fact]
    public async Task GetTokenUsage_ForNewProject_ReturnsZeroedTotals()
    {
        var projectId = await CreateProjectAsync();

        var response = await Msvc.Api.GetProjectTokenUsageAsync(ValidToken, projectId);
        var content = await ReadContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(content);
        var data = doc.RootElement.GetProperty("data");
        Assert.True(data.TryGetProperty("totals", out var totals));
        Assert.Equal(0, totals.GetProperty("inputTokens").GetInt32());
        Assert.Equal(0, totals.GetProperty("outputTokens").GetInt32());
    }

    [Fact]
    public async Task GetTokenUsage_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Msvc.Api.GetProjectTokenUsageAsync("", Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion
}
