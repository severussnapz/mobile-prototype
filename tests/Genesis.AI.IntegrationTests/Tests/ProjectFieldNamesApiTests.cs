using System.Net;
using System.Text.Json;

namespace Genesis.AI.IntegrationTests.Tests;

public class ProjectFieldNamesApiTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public ProjectFieldNamesApiTests()
    {
        _factory = new TestWebApplicationFactory();
    }

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task CreateProject_ResponseContainsAllProjectResourceFields()
    {
        var client = _factory.CreateAdminClient();
        var createContent = new StringContent(
            """{"code":"FNCRT1","name":"Field Names Create Test","description":"Created by integration test","timeSheetCode":"PORTASK0001045","complianceDomain":"Generic"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var createResponse = await client.PostAsync("/api/v1/projects", createContent);
        var createBody = await createResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var createDoc = JsonDocument.Parse(createBody);
        var projectId = createDoc.RootElement.GetProperty("data").GetProperty("id").GetString();

        var updateGitHubContent = new StringContent(
            """{"gitHubApiRepoUrl":"https://github.com/org/api-repo","gitHubAppRepoUrl":"https://github.com/org/app-repo","figmaFileUrl":"https://www.figma.com/file/abc123/Test","figmaPat":null}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var updateGitHubResponse = await client.PatchAsync($"/api/v1/projects/{projectId}/github", updateGitHubContent);
        Assert.Equal(HttpStatusCode.OK, updateGitHubResponse.StatusCode);

        var updateP00Content = new StringContent(
            """{"releaseType":"Minor","assuranceRequired":true,"pilotDeploymentProcess":"Standard","csoRoleAssigned":true,"igOwnerRoleAssigned":true,"securityReviewerAssigned":true,"medicalDeviceFlag":false}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var updateP00Response = await client.PatchAsync($"/api/v1/projects/{projectId}/p00", updateP00Content);
        Assert.Equal(HttpStatusCode.OK, updateP00Response.StatusCode);

        var response = await client.GetAsync($"/api/v1/projects/{projectId}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("data", out var data), "data missing from response");

        Assert.True(data.TryGetProperty("id", out _), "id missing from response");
        Assert.True(data.TryGetProperty("code", out _), "code missing from response");
        Assert.True(data.TryGetProperty("name", out _), "name missing from response");
        Assert.True(data.TryGetProperty("description", out _), "description missing from response");
        Assert.True(data.TryGetProperty("timeSheetCode", out _), "timeSheetCode missing from response");
        Assert.True(data.TryGetProperty("complianceDomain", out _), "complianceDomain missing from response");
        Assert.True(data.TryGetProperty("status", out _), "status missing from response");
        Assert.True(data.TryGetProperty("createdBy", out _), "createdBy missing from response");
        Assert.True(data.TryGetProperty("createdAt", out _), "createdAt missing from response");
        Assert.True(data.TryGetProperty("updatedAt", out _), "updatedAt missing from response");
        Assert.True(data.TryGetProperty("figmaPatConfigured", out _), "figmaPatConfigured missing from response");

        Assert.True(data.TryGetProperty("gitHubApiRepoUrl", out var gitHubApiRepoUrl), "gitHubApiRepoUrl missing from response");
        Assert.Equal("https://github.com/org/api-repo", gitHubApiRepoUrl.GetString());

        Assert.True(data.TryGetProperty("gitHubAppRepoUrl", out var gitHubAppRepoUrl), "gitHubAppRepoUrl missing from response");
        Assert.Equal("https://github.com/org/app-repo", gitHubAppRepoUrl.GetString());

        Assert.True(data.TryGetProperty("figmaFileUrl", out var figmaFileUrl), "figmaFileUrl missing from response");
        Assert.Equal("https://www.figma.com/file/abc123/Test", figmaFileUrl.GetString());

        if (data.TryGetProperty("figmaPatHint", out var figmaPatHint))
        {
            Assert.Equal(JsonValueKind.Null, figmaPatHint.ValueKind);
        }

        Assert.True(data.TryGetProperty("releaseType", out var releaseType), "releaseType missing from response");
        Assert.Equal("Minor", releaseType.GetString());

        Assert.True(data.TryGetProperty("assuranceRequired", out var assuranceRequired), "assuranceRequired missing from response");
        Assert.True(assuranceRequired.GetBoolean());

        Assert.True(data.TryGetProperty("pilotDeploymentProcess", out var pilotDeploymentProcess), "pilotDeploymentProcess missing from response");
        Assert.Equal("Standard", pilotDeploymentProcess.GetString());

        Assert.True(data.TryGetProperty("csoRoleAssigned", out var csoRoleAssigned), "csoRoleAssigned missing from response");
        Assert.True(csoRoleAssigned.GetBoolean());

        Assert.True(data.TryGetProperty("igOwnerRoleAssigned", out var igOwnerRoleAssigned), "igOwnerRoleAssigned missing from response");
        Assert.True(igOwnerRoleAssigned.GetBoolean());

        Assert.True(data.TryGetProperty("securityReviewerAssigned", out var securityReviewerAssigned), "securityReviewerAssigned missing from response");
        Assert.True(securityReviewerAssigned.GetBoolean());

        Assert.True(data.TryGetProperty("medicalDeviceFlag", out var medicalDeviceFlag), "medicalDeviceFlag missing from response");
        Assert.False(medicalDeviceFlag.GetBoolean());

        Assert.True(data.TryGetProperty("pipelineStages", out var pipelineStages), "pipelineStages missing from response");

        Assert.True(pipelineStages.ValueKind == JsonValueKind.Array, "pipelineStages missing from response");
        Assert.True(pipelineStages.GetArrayLength() > 0, "pipelineStages missing from response");

        var firstStage = pipelineStages[0];
        Assert.True(firstStage.TryGetProperty("id", out _), "id missing from response");
        Assert.True(firstStage.TryGetProperty("stageType", out _), "stageType missing from response");
        Assert.True(firstStage.TryGetProperty("status", out _), "status missing from response");
        Assert.True(firstStage.TryGetProperty("iteration", out _), "iteration missing from response");
        Assert.True(firstStage.TryGetProperty("sortOrder", out _), "sortOrder missing from response");
    }

    [Fact]
    public async Task GetProject_ResponseContainsAllProjectResourceFields()
    {
        var client = _factory.CreateAdminClient();
        var createContent = new StringContent(
            """{"code":"FNGET1","name":"Field Names Get Test","description":"Created by integration test","timeSheetCode":"PORTASK0001045","complianceDomain":"Generic"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var createResponse = await client.PostAsync("/api/v1/projects", createContent);
        var createBody = await createResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        using var createDoc = JsonDocument.Parse(createBody);
        var projectId = createDoc.RootElement.GetProperty("data").GetProperty("id").GetString();

        var updateGitHubContent = new StringContent(
            """{"gitHubApiRepoUrl":"https://github.com/org/api-repo","gitHubAppRepoUrl":"https://github.com/org/app-repo","figmaFileUrl":"https://www.figma.com/file/abc123/Test","figmaPat":null}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var updateGitHubResponse = await client.PatchAsync($"/api/v1/projects/{projectId}/github", updateGitHubContent);
        Assert.Equal(HttpStatusCode.OK, updateGitHubResponse.StatusCode);

        var updateP00Content = new StringContent(
            """{"releaseType":"Minor","assuranceRequired":true,"pilotDeploymentProcess":"Standard","csoRoleAssigned":true,"igOwnerRoleAssigned":true,"securityReviewerAssigned":true,"medicalDeviceFlag":false}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var updateP00Response = await client.PatchAsync($"/api/v1/projects/{projectId}/p00", updateP00Content);
        Assert.Equal(HttpStatusCode.OK, updateP00Response.StatusCode);

        var response = await client.GetAsync($"/api/v1/projects/{projectId}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("data", out var data), "data missing from response");

        Assert.True(data.TryGetProperty("id", out _), "id missing from response");
        Assert.True(data.TryGetProperty("code", out _), "code missing from response");
        Assert.True(data.TryGetProperty("name", out _), "name missing from response");
        Assert.True(data.TryGetProperty("description", out _), "description missing from response");
        Assert.True(data.TryGetProperty("timeSheetCode", out _), "timeSheetCode missing from response");
        Assert.True(data.TryGetProperty("complianceDomain", out _), "complianceDomain missing from response");
        Assert.True(data.TryGetProperty("status", out _), "status missing from response");
        Assert.True(data.TryGetProperty("createdBy", out _), "createdBy missing from response");
        Assert.True(data.TryGetProperty("createdAt", out _), "createdAt missing from response");
        Assert.True(data.TryGetProperty("updatedAt", out _), "updatedAt missing from response");
        Assert.True(data.TryGetProperty("figmaPatConfigured", out _), "figmaPatConfigured missing from response");

        Assert.True(data.TryGetProperty("gitHubApiRepoUrl", out var gitHubApiRepoUrl), "gitHubApiRepoUrl missing from response");
        Assert.Equal("https://github.com/org/api-repo", gitHubApiRepoUrl.GetString());

        Assert.True(data.TryGetProperty("gitHubAppRepoUrl", out var gitHubAppRepoUrl), "gitHubAppRepoUrl missing from response");
        Assert.Equal("https://github.com/org/app-repo", gitHubAppRepoUrl.GetString());

        Assert.True(data.TryGetProperty("figmaFileUrl", out var figmaFileUrl), "figmaFileUrl missing from response");
        Assert.Equal("https://www.figma.com/file/abc123/Test", figmaFileUrl.GetString());

        if (data.TryGetProperty("figmaPatHint", out var figmaPatHint))
        {
            Assert.Equal(JsonValueKind.Null, figmaPatHint.ValueKind);
        }

        Assert.True(data.TryGetProperty("releaseType", out var releaseType), "releaseType missing from response");
        Assert.Equal("Minor", releaseType.GetString());

        Assert.True(data.TryGetProperty("assuranceRequired", out var assuranceRequired), "assuranceRequired missing from response");
        Assert.True(assuranceRequired.GetBoolean());

        Assert.True(data.TryGetProperty("pilotDeploymentProcess", out var pilotDeploymentProcess), "pilotDeploymentProcess missing from response");
        Assert.Equal("Standard", pilotDeploymentProcess.GetString());

        Assert.True(data.TryGetProperty("csoRoleAssigned", out var csoRoleAssigned), "csoRoleAssigned missing from response");
        Assert.True(csoRoleAssigned.GetBoolean());

        Assert.True(data.TryGetProperty("igOwnerRoleAssigned", out var igOwnerRoleAssigned), "igOwnerRoleAssigned missing from response");
        Assert.True(igOwnerRoleAssigned.GetBoolean());

        Assert.True(data.TryGetProperty("securityReviewerAssigned", out var securityReviewerAssigned), "securityReviewerAssigned missing from response");
        Assert.True(securityReviewerAssigned.GetBoolean());

        Assert.True(data.TryGetProperty("medicalDeviceFlag", out var medicalDeviceFlag), "medicalDeviceFlag missing from response");
        Assert.False(medicalDeviceFlag.GetBoolean());

        Assert.True(data.TryGetProperty("pipelineStages", out var pipelineStages), "pipelineStages missing from response");

        Assert.True(pipelineStages.ValueKind == JsonValueKind.Array, "pipelineStages missing from response");
        Assert.True(pipelineStages.GetArrayLength() > 0, "pipelineStages missing from response");

        var firstStage = pipelineStages[0];
        Assert.True(firstStage.TryGetProperty("id", out _), "id missing from response");
        Assert.True(firstStage.TryGetProperty("stageType", out _), "stageType missing from response");
        Assert.True(firstStage.TryGetProperty("status", out _), "status missing from response");
        Assert.True(firstStage.TryGetProperty("iteration", out _), "iteration missing from response");
        Assert.True(firstStage.TryGetProperty("sortOrder", out _), "sortOrder missing from response");
    }

    [Fact]
    public async Task GetProjects_ResponseContainsProjectListWithFields()
    {
        var client = _factory.CreateAdminClient();
        var createContent = new StringContent(
            """{"code":"FNLST1","name":"Field Names List Test","description":"Created by integration test","timeSheetCode":"PORTASK0001045","complianceDomain":"Generic"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var createResponse = await client.PostAsync("/api/v1/projects", createContent);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var response = await client.GetAsync("/api/v1/projects");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("data", out var data), "data missing from response");
        Assert.Equal(JsonValueKind.Array, data.ValueKind);
        Assert.True(data.GetArrayLength() > 0, "data missing from response");

        var firstProject = data[0];
        Assert.True(firstProject.TryGetProperty("id", out _), "id missing from response");
        Assert.True(firstProject.TryGetProperty("code", out _), "code missing from response");
        Assert.True(firstProject.TryGetProperty("name", out _), "name missing from response");
        Assert.True(firstProject.TryGetProperty("status", out _), "status missing from response");
        Assert.True(firstProject.TryGetProperty("complianceDomain", out _), "complianceDomain missing from response");
        Assert.True(firstProject.TryGetProperty("pipelineStages", out _), "pipelineStages missing from response");
    }
}