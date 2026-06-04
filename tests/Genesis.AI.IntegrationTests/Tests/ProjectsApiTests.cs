using System.Net;
using System.Text.Json;

namespace Genesis.AI.IntegrationTests.Tests;

public class ProjectsApiTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public ProjectsApiTests()
    {
        _factory = new TestWebApplicationFactory();
    }

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetProjects_WhenEmpty_ReturnsEmptyDataArray()
    {
        var client = _factory.CreateAdminClient();

        var response = await client.GetAsync("/api/v1/projects");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("data", out var data));
        Assert.Equal(JsonValueKind.Array, data.ValueKind);
    }

    [Fact]
    public async Task CreateProject_WithValidPayload_Returns201Created()
    {
        var client = _factory.CreateAdminClient();
        var content = new StringContent(
            """{"code":"INTTEST","name":"Integration Test Project","description":"Created by integration test","timeSheetCode":"PORTASK0001045","complianceDomain":"Generic"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/v1/projects", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("data", out var data));
        Assert.True(data.TryGetProperty("id", out _));
        Assert.True(data.TryGetProperty("pipelineStages", out var stages));
        Assert.Equal(8, stages.GetArrayLength());
    }

    [Fact]
    public async Task CreateProject_WithClinicalDomain_OnlyRequirementsDiscoveryNotStarted()
    {
        var client = _factory.CreateAdminClient();
        var content = new StringContent(
            """{"code":"CLIN","name":"Clinical Project","description":"Test","timeSheetCode":"PORTASK0001045","complianceDomain":"ClinicalUk"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/v1/projects", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        var stages = doc.RootElement.GetProperty("data").GetProperty("pipelineStages");
        foreach (var stage in stages.EnumerateArray())
        {
            var stageType = stage.GetProperty("stageType").GetString();
            var status = stage.GetProperty("status").GetString();
            if (string.Equals(stageType, "requirements-discovery", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Equal("not-started", status);
            }
            else
            {
                Assert.Equal("blocked", status);
            }
        }
    }

    [Fact]
    public async Task CreateProject_WithGenericDomain_OnlyRequirementsDiscoveryNotStarted()
    {
        var client = _factory.CreateAdminClient();
        var content = new StringContent(
            """{"code":"GEN","name":"Generic Project","description":"Test","timeSheetCode":"PORTASK0001045","complianceDomain":"Generic"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/v1/projects", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        var stages = doc.RootElement.GetProperty("data").GetProperty("pipelineStages");
        foreach (var stage in stages.EnumerateArray())
        {
            var stageType = stage.GetProperty("stageType").GetString();
            var status = stage.GetProperty("status").GetString();
            if (string.Equals(stageType, "requirements-discovery", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Equal("not-started", status);
            }
            else
            {
                Assert.Equal("blocked", status);
            }
        }
    }

    [Fact]
    public async Task CreateProject_WithInvalidDomain_Returns400BadRequest()
    {
        var client = _factory.CreateAdminClient();
        var content = new StringContent(
            """{"code":"BAD","name":"Bad Domain","description":"Test","timeSheetCode":"PORTASK0001045","complianceDomain":"InvalidDomain"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/v1/projects", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetProject_WithValidId_ReturnsProjectWithStages()
    {
        var client = _factory.CreateAdminClient();

        // Create a project first
        var createContent = new StringContent(
            """{"code":"GET1","name":"Get Test","description":"Test","timeSheetCode":"PORTASK0001045","complianceDomain":"Generic"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var createResponse = await client.PostAsync("/api/v1/projects", createContent);
        var createBody = await createResponse.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createBody);
        var projectId = createDoc.RootElement.GetProperty("data").GetProperty("id").GetString();

        // Get it
        var response = await client.GetAsync($"/api/v1/projects/{projectId}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        var data = doc.RootElement.GetProperty("data");
        Assert.Equal("GET1", data.GetProperty("code").GetString());
        Assert.Equal(8, data.GetProperty("pipelineStages").GetArrayLength());
    }

    [Fact]
    public async Task GetProject_WithNonExistentId_Returns404NotFound()
    {
        var client = _factory.CreateAdminClient();

        var response = await client.GetAsync($"/api/v1/projects/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProject_WithValidId_Returns204NoContent()
    {
        var client = _factory.CreateAdminClient();

        // Create then delete
        var createContent = new StringContent(
            """{"code":"DEL1","name":"Delete Test","description":"Test","timeSheetCode":"PORTASK0001045","complianceDomain":"Generic"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var createResponse = await client.PostAsync("/api/v1/projects", createContent);
        var createBody = await createResponse.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createBody);
        var projectId = createDoc.RootElement.GetProperty("data").GetProperty("id").GetString();

        var response = await client.DeleteAsync($"/api/v1/projects/{projectId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProject_SoftDeletes_ProjectNotReturnedInList()
    {
        var client = _factory.CreateAdminClient();

        // Create
        var createContent = new StringContent(
            """{"code":"SOFT","name":"Soft Delete","description":"Test","timeSheetCode":"PORTASK0001045","complianceDomain":"Generic"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var createResponse = await client.PostAsync("/api/v1/projects", createContent);
        var createBody = await createResponse.Content.ReadAsStringAsync();
        using var createDoc = JsonDocument.Parse(createBody);
        var projectId = createDoc.RootElement.GetProperty("data").GetProperty("id").GetString();

        // Delete
        await client.DeleteAsync($"/api/v1/projects/{projectId}");

        // Verify not in list
        var listResponse = await client.GetAsync("/api/v1/projects");
        var listBody = await listResponse.Content.ReadAsStringAsync();
        using var listDoc = JsonDocument.Parse(listBody);
        var projects = listDoc.RootElement.GetProperty("data");

        foreach (var project in projects.EnumerateArray())
        {
            Assert.NotEqual(projectId, project.GetProperty("id").GetString());
        }
    }
}
