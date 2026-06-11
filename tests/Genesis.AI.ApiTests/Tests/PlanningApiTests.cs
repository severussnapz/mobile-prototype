using System.Net;
using System.Text.Json;
using Genesis.AI.ApiTests.Setup;

namespace Genesis.AI.ApiTests.Tests;

public class PlanningApiTests(GenesisAiFixture fixture) : GenesisAiBaseTest(fixture)
{
    #region Helpers

    private async Task<Guid> CreateProjectAsync()
    {
        var body = new
        {
            code = GenerateProjectCode("PLAN"),
            name = $"Planning Test {DateTime.UtcNow:HHmmss}",
            description = "Created for planning API tests",
            timeSheetCode = "PORTASK0001045",
            complianceDomain = "Generic"
        };
        var response = await Msvc.Api.CreateProjectAsync(ValidToken, body);
        var content = await ReadContentAsync(response);
        using var doc = JsonDocument.Parse(content);
        return doc.RootElement.GetProperty("data").GetProperty("id").GetGuid();
    }

    #endregion

    #region GET /api/v1/projects/{projectId}/planning/status

    [Fact]
    public async Task GetStatus_ForNewProject_ReturnsGateNotPassed()
    {
        var projectId = await CreateProjectAsync();

        var response = await Msvc.Api.GetPlanningStatusAsync(ValidToken, projectId);
        var content = await ReadContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(content);
        Assert.False(doc.RootElement.GetProperty("data").GetProperty("gatePassed").GetBoolean());
    }

    [Fact]
    public async Task GetStatus_ForUnknownProject_ReturnsNotFound()
    {
        var response = await Msvc.Api.GetPlanningStatusAsync(ValidToken, Guid.NewGuid());

        AssertNotFound(response);
    }

    [Fact]
    public async Task GetStatus_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Msvc.Api.GetPlanningStatusAsync("", Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region GET /api/v1/projects/{projectId}/planning/artefacts

    [Fact]
    public async Task GetArtefacts_ForNewProject_ReturnsEmptyArray()
    {
        var projectId = await CreateProjectAsync();

        var response = await Msvc.Api.GetPlanningArtefactsAsync(ValidToken, projectId);
        var content = await ReadContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(content);
        var data = doc.RootElement.GetProperty("data");
        Assert.Equal(JsonValueKind.Array, data.ValueKind);
        Assert.Equal(0, data.GetArrayLength());
    }

    #endregion

    #region POST /api/v1/projects/{projectId}/planning/run-preflight

    [Fact]
    public async Task RunPreflight_ForNewProject_ReturnsOkWithPreflightFailed()
    {
        var projectId = await CreateProjectAsync();

        var response = await Msvc.Api.RunPlanningPreflightAsync(ValidToken, projectId);
        var content = await ReadContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(content);
        Assert.False(doc.RootElement.GetProperty("data").GetProperty("preflightPassed").GetBoolean());
    }

    [Fact]
    public async Task RunPreflight_ForUnknownProject_ReturnsNotFound()
    {
        var response = await Msvc.Api.RunPlanningPreflightAsync(ValidToken, Guid.NewGuid());

        AssertNotFound(response);
    }

    [Fact]
    public async Task RunPreflight_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Msvc.Api.RunPlanningPreflightAsync("", Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region POST /api/v1/projects/{projectId}/planning/approve-em-review

    [Fact]
    public async Task ApproveEmReview_ForUnknownProject_ReturnsNotFound()
    {
        var response = await Msvc.Api.ApproveEmReviewAsync(
            ValidToken, Guid.NewGuid(), new { notes = "Approved" });

        AssertNotFound(response);
    }

    [Fact]
    public async Task ApproveEmReview_WithoutTaskPlan_ReturnsConflict()
    {
        var projectId = await CreateProjectAsync();

        var response = await Msvc.Api.ApproveEmReviewAsync(
            ValidToken, projectId, new { notes = "Approved" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    #endregion

    #region POST /api/v1/projects/{projectId}/planning/split-tasks

    [Fact]
    public async Task SplitTasks_ForUnknownProject_ReturnsNotFound()
    {
        var response = await Msvc.Api.SplitPlanningTasksAsync(ValidToken, Guid.NewGuid());

        AssertNotFound(response);
    }

    [Fact]
    public async Task SplitTasks_WithoutTasksData_ReturnsConflict()
    {
        var projectId = await CreateProjectAsync();

        var response = await Msvc.Api.SplitPlanningTasksAsync(ValidToken, projectId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    #endregion
}
