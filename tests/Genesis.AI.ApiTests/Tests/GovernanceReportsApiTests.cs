using System.Net;
using Genesis.AI.ApiTests.Setup;

namespace Genesis.AI.ApiTests.Tests;

public class GovernanceReportsApiTests(GenesisAiFixture fixture) : GenesisAiBaseTest(fixture)
{
    #region Helpers

    private async Task<Guid> CreateProjectAsync()
    {
        var body = new
        {
            code = GenerateProjectCode("GOV"),
            name = $"Governance Test {DateTime.UtcNow:HHmmss}",
            description = "Created for governance report API tests",
            timeSheetCode = "PORTASK0001045",
            complianceDomain = "Generic"
        };
        var response = await Msvc.Api.CreateProjectAsync(ValidToken, body);
        var content = await ReadContentAsync(response);
        using var doc = System.Text.Json.JsonDocument.Parse(content);
        return doc.RootElement.GetProperty("data").GetProperty("id").GetGuid();
    }

    #endregion

    #region POST /api/v1/projects/{projectId}/data-protection-impact-assessment

    [Fact]
    public async Task GenerateDpia_ForUnknownProject_ReturnsNotFound()
    {
        var response = await Msvc.Api.GenerateDpiaReportAsync(ValidToken, Guid.NewGuid());

        AssertNotFound(response);
    }

    [Fact]
    public async Task GenerateDpia_WithoutSourceData_ReturnsConflict()
    {
        var projectId = await CreateProjectAsync();

        var response = await Msvc.Api.GenerateDpiaReportAsync(ValidToken, projectId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GenerateDpia_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Msvc.Api.GenerateDpiaReportAsync("", Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region POST /api/v1/projects/{projectId}/security-review-report

    [Fact]
    public async Task GenerateSecurityReviewReport_ForUnknownProject_ReturnsNotFound()
    {
        var response = await Msvc.Api.GenerateSecurityReviewReportAsync(ValidToken, Guid.NewGuid());

        AssertNotFound(response);
    }

    [Fact]
    public async Task GenerateSecurityReviewReport_WithoutSourceData_ReturnsConflict()
    {
        var projectId = await CreateProjectAsync();

        var response = await Msvc.Api.GenerateSecurityReviewReportAsync(ValidToken, projectId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GenerateSecurityReviewReport_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Msvc.Api.GenerateSecurityReviewReportAsync("", Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region POST /api/v1/projects/{projectId}/hazard-log

    [Fact]
    public async Task GenerateHazardLog_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Msvc.Api.GenerateHazardLogAsync("", Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GenerateHazardLog_ForUnknownProject_ReturnsNotFoundOrForbidden()
    {
        var response = await Msvc.Api.GenerateHazardLogAsync(ValidToken, Guid.NewGuid());

        Assert.True(
            response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden,
            $"Expected NotFound (clinical scope) or Forbidden (no clinical scope) but got {response.StatusCode}");
    }

    [Fact]
    public async Task GenerateHazardLog_WithoutRegistry_ReturnsConflictOrForbidden()
    {
        var projectId = await CreateProjectAsync();

        var response = await Msvc.Api.GenerateHazardLogAsync(ValidToken, projectId);

        Assert.True(
            response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.Forbidden,
            $"Expected Conflict (clinical scope) or Forbidden (no clinical scope) but got {response.StatusCode}");
    }

    #endregion
}
