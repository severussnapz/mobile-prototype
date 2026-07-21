using System.Net;
using System.Text;
using System.Text.Json;

namespace Genesis.AI.IntegrationTests.Tests;

public class NormalisationApiTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public NormalisationApiTests()
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
            $"{{\"code\":\"{code}\",\"name\":\"Normalisation Test\",\"description\":\"Test\",\"timeSheetCode\":\"PORTASK0001045\",\"complianceDomain\":\"Generic\"}}",
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

    [Fact]
    public async Task RunExtractRequirements_ProjectNotFound_Returns404NotFound()
    {
        var client = _factory.CreateAdminClient();

        var response = await client.PostAsync($"/api/v1/projects/{Guid.NewGuid()}/normalisation/extract-requirements", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RunExtractRequirements_MissingManifestOrRequirements_Returns409Conflict()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client, "NORM00");

        var response = await client.PostAsync($"/api/v1/projects/{projectId}/normalisation/extract-requirements", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RunExtractRequirements_MissingSecurityDependencies_ReturnsOkWithGateWarnings()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client, "NORM03");

        await AddArtefactsAsync(
            client,
            projectId,
            ("manifest.md", "{}"),
            ("requirements/REQ-001.md", "# Requirement"));

        var response = await client.PostAsync($"/api/v1/projects/{projectId}/normalisation/extract-requirements", null);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var runStatus = document.RootElement
            .GetProperty("data")
            .GetProperty("runStatus")
            .GetString();
        var gatePassed = document.RootElement
            .GetProperty("data")
            .GetProperty("gatePassed")
            .GetBoolean();
        var errors = document.RootElement
            .GetProperty("data")
            .GetProperty("errors")
            .EnumerateArray()
            .Select(element => element.GetString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToList();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("prerequisites_met", runStatus);
        Assert.False(gatePassed);
        Assert.Contains(errors, error => error.Contains("output/SECURITY_ASSURANCE_DATA.json", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("output/SDP_EVIDENCE.json", StringComparison.Ordinal));
    }

    [Fact]
    public async Task VerifyComplete_MissingOutputs_ReturnsGateFailed()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client, "NORM01");

        var response = await client.PostAsync($"/api/v1/projects/{projectId}/normalisation/verify-complete", null);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var gatePassed = document.RootElement
            .GetProperty("data")
            .GetProperty("gatePassed")
            .GetBoolean();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(gatePassed);
    }

    [Fact]
    public async Task VerifyComplete_RequiredOutputsPresent_ReturnsGatePassed()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client, "NORM02");

        await AddArtefactsAsync(
            client,
            projectId,
            ("manifest.md", "{}"),
            ("requirements/REQ-001.md", "# Requirement"),
            ("output/SECURITY_ASSURANCE_DATA.json", "{}"),
            ("output/SDP_EVIDENCE.json", "{}"),
            ("output/REQ-001/checks.json", "{}"),
            ("output/REQ-001/hazards.json", "{}"),
            ("output/REQ-001/api_contracts.json", "{}"),
            ("output/REQ-001/schema.json", "{}"),
            ("output/REQ-001/interfaces.json", "{}"),
            ("output/REQ-001/components.json", "{}"),
            ("output/REQ-001/observability.json", "{}"),
            ("output/cross_cutting/traceability.json", "{}"),
            ("output/cross_cutting/dependency_graph.json", "{}"),
            ("output/cross_cutting/last_extracted.json", "{}"),
            ("output/CS_Guardrails.json", "{}"));

        var response = await client.PostAsync($"/api/v1/projects/{projectId}/normalisation/verify-complete", null);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var gatePassed = document.RootElement
            .GetProperty("data")
            .GetProperty("gatePassed")
            .GetBoolean();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(gatePassed);
    }

    [Fact]
    public async Task BypassPlanningGate_WriteScopeWithoutAdmin_Returns403Forbidden()
    {
        var adminClient = _factory.CreateAdminClient();
        var writeClient = _factory.CreateWriteClient();
        var projectId = await CreateProjectAsync(adminClient, "NORM04");

        var response = await writeClient.PostAsync(
            $"/api/v1/projects/{projectId}/normalisation/bypass-planning-gate",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task BypassPlanningGate_AdminScope_ReturnsPlanningEligibleAndAuditActor()
    {
        var bootstrapClient = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(bootstrapClient, "NORM05");

        var adminToken = _factory.TokenGenerator.CreateBearerToken(
            Genesis.AI.TestFramework.MockTokenGenerator.TestUserErn,
            "Test User",
            "genai-req.admin",
            "genai-req.read",
            "genai-req.write");
        var client = _factory.CreateClientWithToken(adminToken);

        var response = await client.PostAsync(
            $"/api/v1/projects/{projectId}/normalisation/bypass-planning-gate",
            new StringContent("{\"reason\":\"Bypass for planning progression\"}", Encoding.UTF8, "application/json"));
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);

        var planningEligible = document.RootElement
            .GetProperty("data")
            .GetProperty("planningEligible")
            .GetBoolean();
        var bypassActive = document.RootElement
            .GetProperty("data")
            .GetProperty("bypassActive")
            .GetBoolean();
        var bypassedBy = document.RootElement
            .GetProperty("data")
            .GetProperty("bypassedBy")
            .GetString();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(planningEligible);
        Assert.True(bypassActive);
        Assert.False(string.IsNullOrWhiteSpace(bypassedBy));
    }
}
