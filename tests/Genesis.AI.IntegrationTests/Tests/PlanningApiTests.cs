using System.Net;
using System.Text;
using System.Text.Json;

namespace Genesis.AI.IntegrationTests.Tests;

public class PlanningApiTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public PlanningApiTests()
    {
        _factory = new TestWebApplicationFactory();
    }

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private static async Task<string> CreateProjectAsync(HttpClient client, string code)
    {
        var content = new StringContent(
            $"{{\"code\":\"{code}\",\"name\":\"Planning Test\",\"description\":\"Test\",\"timeSheetCode\":\"PORTASK0001045\",\"complianceDomain\":\"Generic\"}}",
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/v1/projects", content);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("data").GetProperty("id").GetString()!;
    }

    private static async Task AddArtefactsAsync(HttpClient client, string projectId, params (string FilePath, string Content)[] artefacts)
    {
        var payload = new
        {
            artefacts = artefacts.Select(artefact => new
            {
                filePath = artefact.FilePath,
                contentType = "application/json",
                content = artefact.Content
            }).ToArray()
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        await client.PostAsync($"/api/v1/projects/{projectId}/artefacts", content);
    }

    // ─── Run Preflight ────────────────────────────────────────────────────────

    [Fact]
    public async Task RunPreflight_ProjectNotFound_Returns404()
    {
        var client = _factory.CreateAdminClient();

        var response = await client.PostAsync($"/api/v1/projects/{Guid.NewGuid()}/planning/run-preflight", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RunPreflight_NoArtefacts_ReturnsOkWithErrors()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client, "PLAN00");

        var response = await client.PostAsync($"/api/v1/projects/{projectId}/planning/run-preflight", null);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var preflightPassed = document.RootElement.GetProperty("data").GetProperty("preflightPassed").GetBoolean();
        var errors = document.RootElement.GetProperty("data").GetProperty("errors").EnumerateArray().ToList();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(preflightPassed);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public async Task RunPreflight_AllPrerequisitesPresent_ReturnsOkWithPreflightPassed()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client, "PLAN01");

        await AddArtefactsAsync(
            client,
            projectId,
            ("output/planning/Task_Plan.md", "# Task Plan"),
            ("output/planning/tasks_data.json", "{\"tasks\":[{\"id\":\"TASK-001\",\"context\":{\"checks_embedded\":[]}}]}"),
            ("output/planning/EM_APPROVAL.json", "{\"approvedBy\":\"user\",\"taskPlanVersion\":1,\"tasksDataVersion\":1}"),
            ("output/tasks/task_index.json", "{\"tasks\":[{\"id\":\"TASK-001\"}]}"),
            ("output/tasks/TASK-001.json", "{\"id\":\"TASK-001\"}"),
            ("output/tasks/SPLIT_STATUS.json", "{\"status\":\"passed\",\"taskCount\":1}"));

        var response = await client.PostAsync($"/api/v1/projects/{projectId}/planning/run-preflight", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ─── Approve EM Review ────────────────────────────────────────────────────

    [Fact]
    public async Task ApproveEmReview_ProjectNotFound_Returns404()
    {
        var client = _factory.CreateAdminClient();

        var response = await client.PostAsync(
            $"/api/v1/projects/{Guid.NewGuid()}/planning/approve-em-review",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ApproveEmReview_TaskPlanMissing_Returns409()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client, "PLAN02");

        var response = await client.PostAsync(
            $"/api/v1/projects/{projectId}/planning/approve-em-review",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ApproveEmReview_TasksDataMissing_Returns409()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client, "PLAN03");

        await AddArtefactsAsync(client, projectId, ("output/planning/Task_Plan.md", "# Plan"));

        var response = await client.PostAsync(
            $"/api/v1/projects/{projectId}/planning/approve-em-review",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ApproveEmReview_BothArtefactsPresent_ReturnsOkWithApprovalState()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client, "PLAN04");

        await AddArtefactsAsync(
            client,
            projectId,
            ("output/planning/Task_Plan.md", "# Plan"),
            ("output/planning/tasks_data.json", "{\"tasks\":[{\"id\":\"TASK-001\"}]}"));

        var response = await client.PostAsync(
            $"/api/v1/projects/{projectId}/planning/approve-em-review",
            new StringContent("{\"notes\":\"LGTM\"}", Encoding.UTF8, "application/json"));
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var emApproved = document.RootElement.GetProperty("data").GetProperty("emApproved").GetBoolean();
        var approvedBy = document.RootElement.GetProperty("data").GetProperty("approvedBy").GetString();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(emApproved);
        Assert.False(string.IsNullOrWhiteSpace(approvedBy));
    }

    // ─── Split Tasks ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SplitTasks_ProjectNotFound_Returns404()
    {
        var client = _factory.CreateAdminClient();

        var response = await client.PostAsync($"/api/v1/projects/{Guid.NewGuid()}/planning/split-tasks", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SplitTasks_MissingTasksData_Returns409()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client, "PLAN05");

        var response = await client.PostAsync($"/api/v1/projects/{projectId}/planning/split-tasks", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task SplitTasks_EmApprovalMissing_Returns409()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client, "PLAN06");

        await AddArtefactsAsync(client, projectId, ("output/planning/tasks_data.json", "{\"tasks\":[{\"id\":\"TASK-001\",\"context\":{\"checks_embedded\":[]}}]}"));

        var response = await client.PostAsync($"/api/v1/projects/{projectId}/planning/split-tasks", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task SplitTasks_EmApprovalStale_Returns409()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client, "PLAN07");

        // Version 1 of tasks_data approved, then we add a new version
        await AddArtefactsAsync(
            client,
            projectId,
            ("output/planning/Task_Plan.md", "# Plan"),
            ("output/planning/tasks_data.json", "{\"tasks\":[{\"id\":\"TASK-001\",\"context\":{\"checks_embedded\":[]}}]}"),
            ("output/planning/EM_APPROVAL.json", "{\"approvedBy\":\"user\",\"taskPlanVersion\":1,\"tasksDataVersion\":1}"));

        // Simulate regeneration by saving tasks_data again (version becomes 2)
        await AddArtefactsAsync(client, projectId, ("output/planning/tasks_data.json", "{\"tasks\":[{\"id\":\"TASK-002\",\"context\":{\"checks_embedded\":[]}}]}"));

        var response = await client.PostAsync($"/api/v1/projects/{projectId}/planning/split-tasks", null);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("Re-approve", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SplitTasks_ValidApprovedData_ReturnsOkAndCreatesTaskFiles()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client, "PLAN08");

        var tasksDataJson = JsonSerializer.Serialize(new
        {
            tasks = new[]
            {
                new { id = "TASK-001", title = "Foundation", layer = 0, context = new { checks_embedded = Array.Empty<string>() } },
                new { id = "TASK-002", title = "API Layer", layer = 1, context = new { checks_embedded = Array.Empty<string>() } }
            }
        });

        await AddArtefactsAsync(
            client,
            projectId,
            ("output/planning/Task_Plan.md", "# Plan"),
            ("output/planning/tasks_data.json", tasksDataJson));

        // Approve for version 1
        await client.PostAsync(
            $"/api/v1/projects/{projectId}/planning/approve-em-review",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        var response = await client.PostAsync($"/api/v1/projects/{projectId}/planning/split-tasks", null);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var taskCount = document.RootElement.GetProperty("data").GetProperty("taskCount").GetInt32();
        var outputArtefacts = document.RootElement.GetProperty("data").GetProperty("outputArtefacts").EnumerateArray().ToList();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, taskCount);
        Assert.Contains(outputArtefacts, artefact => artefact.GetProperty("filePath").GetString()!.Contains("task_index.json", StringComparison.Ordinal));
        Assert.Contains(outputArtefacts, artefact => artefact.GetProperty("filePath").GetString()!.Contains("SPLIT_STATUS.json", StringComparison.Ordinal));
        Assert.Contains(outputArtefacts, artefact => artefact.GetProperty("filePath").GetString()!.Contains("TASK-001.json", StringComparison.Ordinal));
    }

    // ─── Get Status ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStatus_ProjectNotFound_Returns404()
    {
        var client = _factory.CreateAdminClient();

        var response = await client.GetAsync($"/api/v1/projects/{Guid.NewGuid()}/planning/status");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetStatus_NewProject_ReturnsAllFalseGate()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client, "PLAN09");

        var response = await client.GetAsync($"/api/v1/projects/{projectId}/planning/status");
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var data = document.RootElement.GetProperty("data");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(data.GetProperty("gatePassed").GetBoolean());
        Assert.False(data.GetProperty("preflightPassed").GetBoolean());
        Assert.False(data.GetProperty("emApproved").GetBoolean());
        Assert.False(data.GetProperty("splitPassed").GetBoolean());
    }
}
