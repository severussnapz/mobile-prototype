using System.Net;
using System.Text.Json;
using Genesis.AI.ApiTests.Setup;

namespace Genesis.AI.ApiTests.Tests;

public class ProjectsApiTests(GenesisAiFixture fixture) : GenesisAiBaseTest(fixture)
{
    #region GET /api/v1/projects

    [Fact]
    public async Task GetProjects_WithValidToken_ReturnsOk()
    {

        var response = await Msvc.Api.GetProjectsAsync(ValidToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetProjects_WithValidToken_ReturnsJsonResponse()
    {

        var response = await Msvc.Api.GetProjectsAsync(ValidToken);
        var content = await ReadContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.ContentHeaders?.ContentType?.MediaType);

        using var doc = JsonDocument.Parse(content);
        Assert.True(doc.RootElement.TryGetProperty("data", out var dataElement));
        Assert.Equal(JsonValueKind.Array, dataElement.ValueKind);
    }

    [Fact]
    public async Task GetProjects_WithoutToken_ReturnsUnauthorized()
    {
        var unauthenticatedMsvc = new Clients.GenesisAiMsvc(Fixture.Environment);

        var response = await unauthenticatedMsvc.Api.GetProjectsAsync("");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProjects_WithStatusFilter_ReturnsOk()
    {

        var response = await Msvc.Api.GetProjectsAsync(ValidToken, "discovery");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region POST /api/v1/projects

    [Fact]
    public async Task CreateProject_WithValidPayload_ReturnsCreated()
    {
        var body = new
        {
            code = GenerateProjectCode("TST"),
            name = $"API Test Project {DateTime.UtcNow:HHmmss}",
            description = "Created by automated API test",
            timeSheetCode = "PORTASK0001045",
            complianceDomain = "Generic"
        };

        var response = await Msvc.Api.CreateProjectAsync(ValidToken, body);
        var content = await ReadContentAsync(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var doc = JsonDocument.Parse(content);
        Assert.True(doc.RootElement.TryGetProperty("data", out var dataElement));
        Assert.True(dataElement.TryGetProperty("id", out _));
        Assert.True(dataElement.TryGetProperty("pipelineStages", out var stagesElement));
        Assert.Equal(10, stagesElement.GetArrayLength());
    }

    [Fact]
    public async Task CreateProject_WithInvalidComplianceDomain_ReturnsBadRequest()
    {
        var body = new
        {
            code = "INVALID",
            name = "Invalid Project",
            description = "Testing invalid compliance domain",
            timeSheetCode = "PORTASK0001045",
            complianceDomain = "NotADomain"
        };

        var response = await Msvc.Api.CreateProjectAsync(ValidToken, body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProject_WithoutToken_ReturnsUnauthorized()
    {
        var body = new
        {
            code = "NOAUTH",
            name = "No Auth Project",
            description = "Should fail auth",
            timeSheetCode = "PORTASK0001045",
            complianceDomain = "Generic"
        };

        var response = await Msvc.Api.CreateProjectAsync("", body);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region GET /api/v1/projects/{id}

    [Fact]
    public async Task GetProject_WithNonExistentId_ReturnsNotFound()
    {
        var nonExistentId = Guid.NewGuid();

        var response = await Msvc.Api.GetProjectAsync(ValidToken, nonExistentId);

        AssertNotFound(response);
    }

    [Fact]
    public async Task GetProject_WithValidId_ReturnsProjectWithStages()
    {
        // Arrange — create a project first
        var createBody = new
        {
            code = GenerateProjectCode("GP"),
            name = "Get Project Test",
            description = "Testing GET by ID",
            timeSheetCode = "PORTASK0001045",
            complianceDomain = "Generic"
        };
        var createResponse = await Msvc.Api.CreateProjectAsync(ValidToken, createBody);
        var createContent = await ReadContentAsync(createResponse);
        using var createDoc = JsonDocument.Parse(createContent);
        var projectId = createDoc.RootElement.GetProperty("data").GetProperty("id").GetGuid();

        var response = await Msvc.Api.GetProjectAsync(ValidToken, projectId);
        var content = await ReadContentAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(content);
        var data = doc.RootElement.GetProperty("data");
        Assert.Equal(projectId, data.GetProperty("id").GetGuid());
        Assert.True(data.TryGetProperty("pipelineStages", out _));
    }

    #endregion

    #region DELETE /api/v1/projects/{id}

    [Fact]
    public async Task DeleteProject_WithValidId_ReturnsNoContent()
    {
        // Arrange — create a project to delete
        var createBody = new
        {
            code = GenerateProjectCode("DEL"),
            name = "Delete Test Project",
            description = "Will be soft-deleted",
            timeSheetCode = "PORTASK0001045",
            complianceDomain = "Generic"
        };
        var createResponse = await Msvc.Api.CreateProjectAsync(ValidToken, createBody);
        var createContent = await ReadContentAsync(createResponse);
        using var createDoc = JsonDocument.Parse(createContent);
        var projectId = createDoc.RootElement.GetProperty("data").GetProperty("id").GetGuid();

        var response = await Msvc.Api.DeleteProjectAsync(ValidToken, projectId);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProject_WithNonExistentId_ReturnsNotFound()
    {
        var nonExistentId = Guid.NewGuid();

        var response = await Msvc.Api.DeleteProjectAsync(ValidToken, nonExistentId);

        AssertNotFound(response);
    }

    #endregion

    #region GET /api/v1/projects/{id}/parking-lot

    [Fact]
    public async Task GetProjectParkingLot_WithValidId_ReturnsOk()
    {
        // Arrange — create a project
        var createBody = new
        {
            code = GenerateProjectCode("PL"),
            name = "Parking Lot Test",
            description = "Testing parking lot aggregation",
            timeSheetCode = "PORTASK0001045",
            complianceDomain = "Generic"
        };
        var createResponse = await Msvc.Api.CreateProjectAsync(ValidToken, createBody);
        var createContent = await ReadContentAsync(createResponse);
        using var createDoc = JsonDocument.Parse(createContent);
        var projectId = createDoc.RootElement.GetProperty("data").GetProperty("id").GetGuid();

        var response = await Msvc.Api.GetProjectParkingLotAsync(ValidToken, projectId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion
}
