using System.Net;
using System.Text;
using System.Text.Json;

namespace Genesis.AI.IntegrationTests.Tests;

public class DataProtectionImpactAssessmentApiTests : IDisposable
{
    private const string WordContentType =
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    private const string SampleDpiaJson = """
        {
          "document_version": "1.0",
          "project": {
            "title": "Patient Access",
            "request_date": "2026-06-09",
            "contact_name": "Jane Doe",
            "sponsor": "Clinical Ops",
            "business_unit": "Platform",
            "proposition": "Portal",
            "environment": "Production",
            "stakeholders": ["IG Team"],
            "change_type": "existing_change",
            "summary": "Summary",
            "data_flow": "Flow"
          },
          "processing": {
            "personal_data": true,
            "special_category_data": true,
            "minors_data": false,
            "volume": "Medium",
            "frequency": "Daily",
            "role": "Processor",
            "data_controller": "Controller",
            "data_subjects": ["Patients"],
            "recipients": ["Support"],
            "third_parties": ["Supplier A"]
          },
          "data_profile": {
            "classifications": ["PHI"],
            "data_categories": ["Clinical"],
            "retention_rule": "8 years",
            "deletion_trigger": "Contract end",
            "sharing_methods": ["API"],
            "encryption_at_rest": "AES-256",
            "encryption_in_transit": "TLS1.2+"
          },
          "legal_basis": {
            "article6": "6(1)(e)",
            "article9": "9(2)(h)",
            "lawful_purpose": "Care delivery",
            "privacy_notice_reference": "PN-001"
          },
          "risk_assessment": {
            "risks": [
              {
                "risk_id": "IG-RISK-001",
                "title": "Misuse",
                "description": "Unauthorized access",
                "likelihood": "Possible",
                "impact": "High",
                "controls": ["RBAC"],
                "residual_risk": "Low",
                "check_ids": ["CHECK-001"]
              }
            ]
          },
          "signoff": {
            "ig_reviewer": "Alex IG",
            "role": "DPO",
            "decision": "approved",
            "reference": "REF-1",
            "date": "2026-06-09"
          },
          "source_mapping": [
            {
              "control_id": "IG-001",
              "source_document": "IF15937",
              "source_section": "Section 2"
            }
          ]
        }
        """;

    private readonly TestWebApplicationFactory _factory;

    public DataProtectionImpactAssessmentApiTests()
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
            """{"code":"DPIA","name":"DPIA Test","description":"Test","timeSheetCode":"TS-001","complianceDomain":"ClinicalUk"}""",
            Encoding.UTF8,
            "application/json");
        var response = await client.PostAsync("/api/v1/projects", content);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("data").GetProperty("id").GetString()!;
    }

    private static async Task SeedDpiaArtefactAsync(HttpClient client, string projectId)
    {
        var payload = JsonSerializer.Serialize(new
        {
            artefacts = new[]
            {
                new
                {
                    filePath = "output/PR1625_DPIA_DATA.json",
                    contentType = "application/json",
                    content = SampleDpiaJson
                }
            }
        });

        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        await client.PostAsync($"/api/v1/projects/{projectId}/artefacts", content);
    }

    [Fact]
    public async Task Generate_ProjectDoesNotExist_Returns404NotFound()
    {
        var client = _factory.CreateAdminClient();

        var response = await client.PostAsync(
            $"/api/v1/projects/{Guid.NewGuid()}/data-protection-impact-assessment",
            content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Generate_DpiaDataMissing_Returns409Conflict()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);

        var response = await client.PostAsync(
            $"/api/v1/projects/{projectId}/data-protection-impact-assessment",
            content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Generate_DpiaDataExists_ReturnsOkWithDocxContentType()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);
        await SeedDpiaArtefactAsync(client, projectId);

        var response = await client.PostAsync(
            $"/api/v1/projects/{projectId}/data-protection-impact-assessment",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(WordContentType, response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Generate_DpiaDataExists_ReturnsNonEmptyDocx()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);
        await SeedDpiaArtefactAsync(client, projectId);

        var response = await client.PostAsync(
            $"/api/v1/projects/{projectId}/data-protection-impact-assessment",
            content: null);

        var bytes = await response.Content.ReadAsByteArrayAsync();

        Assert.NotEmpty(bytes);
        Assert.Equal((byte)'P', bytes[0]);
        Assert.Equal((byte)'K', bytes[1]);
    }
}
