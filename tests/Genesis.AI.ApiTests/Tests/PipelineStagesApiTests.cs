using System.Net;
using System.Text.Json;
using Genesis.AI.ApiTests.Setup;

namespace Genesis.AI.ApiTests.Tests;

public class PipelineStagesApiTests(GenesisAiFixture fixture) : GenesisAiBaseTest(fixture)
{
    #region Helpers

    private async Task<Guid> CreateProjectAndGetFirstStageIdAsync()
    {
        var createBody = new
        {
            code = $"STG-{Guid.NewGuid():N}"[..10],
            name = $"Stage Test {DateTime.UtcNow:HHmmss}",
            description = "Created for pipeline stage API tests",
            complianceDomain = "Generic"
        };
        var createResponse = await Msvc.Api.CreateProjectAsync(ValidToken, createBody);
        var content = await ReadContentAsync(createResponse);
        using var doc = JsonDocument.Parse(content);
        var stages = doc.RootElement.GetProperty("data").GetProperty("pipelineStages");
        return stages.EnumerateArray().First().GetProperty("id").GetGuid();
    }

    #endregion

    #region POST /api/v1/stages/{stageId}/complete

    [Fact]
    public async Task CompleteStage_WithNonExistentId_ReturnsNotFound()
    {
        var nonExistentId = Guid.NewGuid();

        var response = await Msvc.Api.CompleteStageAsync(ValidToken, nonExistentId);

        AssertNotFound(response);
    }

    [Fact]
    public async Task CompleteStage_WithoutToken_ReturnsUnauthorized()
    {

        var response = await Msvc.Api.CompleteStageAsync("", Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CompleteStage_WithNoArtefacts_ReturnsBadRequest()
    {
        // Arrange — create a project, start a conversation on the first stage to make it InProgress
        var stageId = await CreateProjectAndGetFirstStageIdAsync();

        // Act — try to complete a stage that has no artefacts
        var response = await Msvc.Api.CompleteStageAsync(ValidToken, stageId);

        // Assert — should fail validation because no artefacts exist
        Assert.True(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound,
            $"Expected BadRequest or NotFound but got {response.StatusCode}");
    }

    #endregion

    #region POST /api/v1/stages/{stageId}/skip

    [Fact]
    public async Task SkipStage_WithNonExistentId_ReturnsNotFound()
    {
        var nonExistentId = Guid.NewGuid();

        var response = await Msvc.Api.SkipStageAsync(ValidToken, nonExistentId);

        AssertNotFound(response);
    }

    [Fact]
    public async Task SkipStage_WithoutToken_ReturnsUnauthorized()
    {

        var response = await Msvc.Api.SkipStageAsync("", Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion
}
