using System.Net;
using System.Text.Json;

namespace Genesis.AI.IntegrationTests.Tests;

public class ArtefactsApiTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public ArtefactsApiTests()
    {
        _factory = new TestWebApplicationFactory();
    }

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private static async Task<string> CreateProjectAsync(HttpClient client)
    {
        var content = new StringContent(
            """{"code":"ART","name":"Artefact Test","description":"Test","timeSheetCode":"PORTASK0001045","complianceDomain":"Generic"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var response = await client.PostAsync("/api/v1/projects", content);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("data").GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task GetArtefacts_ForNewProject_ReturnsEmptyArray()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);

        var response = await client.GetAsync($"/api/v1/projects/{projectId}/artefacts");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(0, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task CreateArtefacts_WithValidPayload_ReturnsCreated()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);

        var payload = new StringContent(
            """{"artefacts":[{"filePath":"docs/manifest.md","contentType":"text/markdown","content":"# Manifest\n\nTest artefact."}]}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync($"/api/v1/projects/{projectId}/artefacts", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetArtefacts_AfterCreation_ReturnsArtefacts()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);
        var payload = new StringContent(
            """{"artefacts":[{"filePath":"docs/test.md","contentType":"text/markdown","content":"# Test"}]}""",
            System.Text.Encoding.UTF8,
            "application/json");
        await client.PostAsync($"/api/v1/projects/{projectId}/artefacts", payload);

        var response = await client.GetAsync($"/api/v1/projects/{projectId}/artefacts");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task GetArtefacts_WithoutToken_Returns401Unauthorised()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/projects/{Guid.NewGuid()}/artefacts");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetArtefactById_WithNonExistentId_Returns404NotFound()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);

        var response = await client.GetAsync(
            $"/api/v1/projects/{projectId}/artefacts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DownloadArtefact_WithNonExistentId_Returns404NotFound()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);

        var response = await client.GetAsync(
            $"/api/v1/projects/{projectId}/artefacts/{Guid.NewGuid()}/download");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DownloadArtefact_ForTextOnlyArtefact_Returns404NotFound()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);
        var payload = new StringContent(
            """{"artefacts":[{"filePath":"docs/text.md","contentType":"text/markdown","content":"# Text"}]}""",
            System.Text.Encoding.UTF8,
            "application/json");
        await client.PostAsync($"/api/v1/projects/{projectId}/artefacts", payload);

        var listResponse = await client.GetAsync($"/api/v1/projects/{projectId}/artefacts");
        var listBody = await listResponse.Content.ReadAsStringAsync();
        using var listDoc = JsonDocument.Parse(listBody);
        var artefactId = listDoc.RootElement[0].GetProperty("id").GetString();

        var response = await client.GetAsync(
            $"/api/v1/projects/{projectId}/artefacts/{artefactId}/download");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DownloadArtefact_ForBinaryArtefact_ReturnsFileBytes()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateClinicalProjectAsync(client);
        var binaryArtefactId = await SeedBinaryArtefactAsync(client, projectId);

        var response = await client.GetAsync(
            $"/api/v1/projects/{projectId}/artefacts/{binaryArtefactId}/download");
        var bytes = await response.Content.ReadAsByteArrayAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEmpty(bytes);
    }

    private static async Task<string> SeedBinaryArtefactAsync(HttpClient client, string projectId)
    {
        await SeedHazardRegistryAsync(client, projectId);
        await client.PostAsync($"/api/v1/projects/{projectId}/hazard-log", content: null);

        var listResponse = await client.GetAsync($"/api/v1/projects/{projectId}/artefacts");
        var listBody = await listResponse.Content.ReadAsStringAsync();
        using var listDoc = JsonDocument.Parse(listBody);
        string? binaryArtefactId = null;
        foreach (var artefactElement in listDoc.RootElement.EnumerateArray())
        {
            var filePath = artefactElement.GetProperty("filePath").GetString();
            if (filePath is not null && filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                binaryArtefactId = artefactElement.GetProperty("id").GetString();
                break;
            }
        }

        Assert.NotNull(binaryArtefactId);
        return binaryArtefactId;
    }

    [Fact]
    public async Task DownloadArtefact_WithoutToken_Returns401Unauthorised()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/v1/projects/{Guid.NewGuid()}/artefacts/{Guid.NewGuid()}/download");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<string> CreateClinicalProjectAsync(HttpClient client)
    {
        var content = new StringContent(
            """{"code":"ARTBIN","name":"Artefact Binary Test","description":"Test","timeSheetCode":"PORTASK0001045","complianceDomain":"ClinicalUk"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var response = await client.PostAsync("/api/v1/projects", content);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("data").GetProperty("id").GetString()!;
    }

    private static async Task SeedHazardRegistryAsync(HttpClient client, string projectId)
    {
        const string registry = """
            # Hazard Registry

            ## HAZ-DOC-001: Wrong patient record displayed
            **Source requirement:** REQ-001
            **Status:** Active

            ### HAZ-DOC-001: Wrong patient record displayed
            **Hazard description:** A clinician is shown the wrong patient record.
            **Potential clinical impact:** Harm from decisions against the wrong record.
            **Initial risk:** Major × Possible = **High**
            **Residual risk:** Major × Unlikely = **Low**
            **Residual risk decision:** Acceptable
            **Existing Controls:** Patient banner shows NHS number.

            #### Cause 1: Ambiguous search returns multiple matches
            | Control ID | Category | Description | CLIN Rule | Evidence ID | Status Proof | Additional Comments | Go/Launch Gate |
            | --- | --- | --- | --- | --- | --- | --- | --- |
            | C-001 | HIT Design | Force NHS number confirmation | CLIN-002 | EV-101 | Done | — | Yes |
            """;
        var payload = JsonSerializer.Serialize(new
        {
            artefacts = new[]
            {
                new
                {
                    filePath = "requirements/HAZARD-REGISTRY.md",
                    contentType = "text/markdown",
                    content = registry
                }
            }
        });
        var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
        await client.PostAsync($"/api/v1/projects/{projectId}/artefacts", content);
    }
}
