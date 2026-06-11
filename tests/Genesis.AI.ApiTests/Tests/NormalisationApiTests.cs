using System.Net;
using System.Text.Json;
using Genesis.AI.ApiTests.Setup;

namespace Genesis.AI.ApiTests.Tests;

public class NormalisationApiTests(GenesisAiFixture fixture) : GenesisAiBaseTest(fixture)
{
    #region Helpers

    private async Task<Guid> CreateProjectAsync()
    {
        var body = new
        {
            code = GenerateProjectCode("NORM"),
            name = $"Normalisation Test {DateTime.UtcNow:HHmmss}",
            description = "Created for normalisation API tests",
            timeSheetCode = "PORTASK0001045",
            complianceDomain = "Generic"
        };
        var response = await Msvc.Api.CreateProjectAsync(ValidToken, body);
        var content = await ReadContentAsync(response);
        using var doc = JsonDocument.Parse(content);
        return doc.RootElement.GetProperty("data").GetProperty("id").GetGuid();
    }

    #endregion

    #region GET /api/v1/projects/{projectId}/normalisation/status

    [Fact]
    public async Task GetStatus_ForNewProject_ReturnsGateNotPassed()
    {
        var projectId = await CreateProjectAsync();

        var response = await Msvc.Api.GetNormalisationStatusAsync(ValidToken, projectId);
        var content = await ReadContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(content);
        var data = doc.RootElement.GetProperty("data");
        Assert.False(data.GetProperty("gatePassed").GetBoolean());
    }

    [Fact]
    public async Task GetStatus_ForUnknownProject_ReturnsNotFound()
    {
        var response = await Msvc.Api.GetNormalisationStatusAsync(ValidToken, Guid.NewGuid());

        AssertNotFound(response);
    }

    [Fact]
    public async Task GetStatus_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Msvc.Api.GetNormalisationStatusAsync("", Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region GET /api/v1/projects/{projectId}/normalisation/artefacts

    [Fact]
    public async Task GetArtefacts_ForNewProject_ReturnsEmptyArray()
    {
        var projectId = await CreateProjectAsync();

        var response = await Msvc.Api.GetNormalisationArtefactsAsync(ValidToken, projectId);
        var content = await ReadContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(content);
        var data = doc.RootElement.GetProperty("data");
        Assert.Equal(JsonValueKind.Array, data.ValueKind);
        Assert.Equal(0, data.GetArrayLength());
    }

    #endregion

    #region POST /api/v1/projects/{projectId}/normalisation/extract-requirements

    [Fact]
    public async Task RunExtract_ForUnknownProject_ReturnsNotFound()
    {
        var response = await Msvc.Api.RunNormalisationExtractAsync(ValidToken, Guid.NewGuid());

        AssertNotFound(response);
    }

    [Fact]
    public async Task RunExtract_WithMissingPrerequisites_ReturnsConflict()
    {
        var projectId = await CreateProjectAsync();

        var response = await Msvc.Api.RunNormalisationExtractAsync(ValidToken, projectId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RunExtract_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Msvc.Api.RunNormalisationExtractAsync("", Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region POST /api/v1/projects/{projectId}/normalisation/verify-complete

    [Fact]
    public async Task VerifyComplete_ForNewProject_ReturnsGateFailed()
    {
        var projectId = await CreateProjectAsync();

        var response = await Msvc.Api.VerifyNormalisationCompleteAsync(ValidToken, projectId);
        var content = await ReadContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(content);
        Assert.False(doc.RootElement.GetProperty("data").GetProperty("gatePassed").GetBoolean());
    }

    [Fact]
    public async Task VerifyComplete_ForUnknownProject_ReturnsNotFound()
    {
        var response = await Msvc.Api.VerifyNormalisationCompleteAsync(ValidToken, Guid.NewGuid());

        AssertNotFound(response);
    }

    #endregion

    #region POST /api/v1/projects/{projectId}/normalisation/bypass-planning-gate

    [Fact]
    public async Task BypassPlanningGate_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Msvc.Api.BypassNormalisationPlanningGateAsync(
            "", Guid.NewGuid(), new { reason = "No token" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task BypassPlanningGate_ForUnknownProject_ReturnsNotFoundOrForbidden()
    {
        var response = await Msvc.Api.BypassNormalisationPlanningGateAsync(
            ValidToken, Guid.NewGuid(), new { reason = "Manual override for API test" });

        Assert.True(
            response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden,
            $"Expected NotFound (admin scope) or Forbidden (no admin scope) but got {response.StatusCode}");
    }

    #endregion
}
