using System.Net;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;

namespace Genesis.AI.IntegrationTests.Tests;

public class HazardLogApiTests : IDisposable
{
    private const string SpreadsheetContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private const string SampleRegistry = """
        # Hazard Registry

        ## HAZ-DOC-001: Patient Identification — Wrong patient record displayed
        **Source requirement:** REQ-001-patient-lookup
        **Status:** Active

        ### HAZ-DOC-001: Wrong patient record displayed
        **Hazard description:** A clinician is shown the wrong patient record.
        **Potential clinical impact:** Decisions against the wrong record could cause harm.
        **Initial risk:** Major × Possible = **High**
        **Residual risk:** Major × Unlikely = **Low**
        **Residual risk decision:** Acceptable
        **Existing Controls:** Patient banner shows NHS number.

        #### Cause 1: Ambiguous search returns multiple matches
        | Control ID | Category | Description | CLIN Rule | Evidence ID | Status Proof | Additional Comments | Go/Launch Gate |
        | --- | --- | --- | --- | --- | --- | --- | --- |
        | C-001 | HIT Design | Force NHS number confirmation | CLIN-002 | EV-101 | Done | — | Yes |
        """;

    private readonly TestWebApplicationFactory _factory;

    public HazardLogApiTests()
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
            """{"code":"HAZ","name":"Hazard Test","description":"Test","timeSheetCode":"TS-001","complianceDomain":"ClinicalUk"}""",
            Encoding.UTF8,
            "application/json");
        var response = await client.PostAsync("/api/v1/projects", content);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("data").GetProperty("id").GetString()!;
    }

    private static async Task SeedRegistryArtefactAsync(HttpClient client, string projectId)
    {
        var payload = JsonSerializer.Serialize(new
        {
            artefacts = new[]
            {
                new
                {
                    filePath = "requirements/HAZARD-REGISTRY.md",
                    contentType = "text/markdown",
                    content = SampleRegistry
                }
            }
        });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        await client.PostAsync($"/api/v1/projects/{projectId}/artefacts", content);
    }

    [Fact]
    public async Task GenerateHazardLog_ProjectDoesNotExist_Returns404NotFound()
    {
        var client = _factory.CreateAdminClient();

        var response = await client.PostAsync(
            $"/api/v1/projects/{Guid.NewGuid()}/hazard-log", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GenerateHazardLog_RegistryMissing_Returns409Conflict()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);

        var response = await client.PostAsync(
            $"/api/v1/projects/{projectId}/hazard-log", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GenerateHazardLog_RegistryExists_ReturnsOkWithSpreadsheetContentType()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);
        await SeedRegistryArtefactAsync(client, projectId);

        var response = await client.PostAsync(
            $"/api/v1/projects/{projectId}/hazard-log", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(SpreadsheetContentType, response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GenerateHazardLog_RegistryExists_ReturnsNonEmptyFile()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);
        await SeedRegistryArtefactAsync(client, projectId);

        var response = await client.PostAsync(
            $"/api/v1/projects/{projectId}/hazard-log", content: null);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public async Task GenerateHazardLog_RegistryExists_ReturnsValidXlsxWorkbook()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);
        await SeedRegistryArtefactAsync(client, projectId);

        var response = await client.PostAsync(
            $"/api/v1/projects/{projectId}/hazard-log", content: null);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(workbook.Worksheet("Hazard Log"));
    }

    [Fact]
    public async Task GenerateHazardLog_RegistryExists_ContractRemainsStable()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);
        await SeedRegistryArtefactAsync(client, projectId);

        var response = await client.PostAsync(
            $"/api/v1/projects/{projectId}/hazard-log", content: null);

        var disposition = response.Content.Headers.ContentDisposition;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(SpreadsheetContentType, response.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(disposition);
        Assert.Equal("attachment", disposition!.DispositionType);
        Assert.EndsWith(".xlsx", disposition.FileNameStar ?? disposition.FileName ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateHazardLog_WithoutToken_Returns401Unauthorised()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync(
            $"/api/v1/projects/{Guid.NewGuid()}/hazard-log", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
