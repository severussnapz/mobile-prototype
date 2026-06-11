using System.Net;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;

namespace Genesis.AI.IntegrationTests.Tests;

public class SecurityReviewReportApiTests : IDisposable
{
  private const string SpreadsheetContentType =
    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private const string SampleSecurityAssuranceJson = """
        {
          "document_version": "1.0",
          "project": {
            "name": "Patient Access",
            "summary": "Summary",
            "architecture_context": "Context",
            "data_sensitivity": ["PII"]
          },
          "threat_model": {
            "assets": ["Records"],
            "actors": ["User"],
            "entry_points": ["API"],
            "abuse_cases": ["Privilege escalation"]
          },
          "attack_vector_coverage": {
            "repo_secrets": { "status": "covered", "controls": ["Secret scanning"], "evidence_refs": ["EV-1"] },
            "ci_cd_exposure": { "status": "covered", "controls": ["Pinned actions"], "evidence_refs": ["EV-2"] },
            "supply_chain": { "status": "covered", "controls": ["Dependency review"], "evidence_refs": ["EV-3"] },
            "injection": { "status": "covered", "controls": ["Parameterised access"], "evidence_refs": ["EV-4"] },
            "authn_authz": { "status": "covered", "controls": ["Policy checks"], "evidence_refs": ["EV-5"] },
            "crypto": { "status": "covered", "controls": ["TLS"], "evidence_refs": ["EV-6"] },
            "logging_monitoring": { "status": "covered", "controls": ["Alerting"], "evidence_refs": ["EV-7"] }
          },
          "control_mappings": [
            {
              "control_id": "SEC-001",
              "title": "Protect secrets",
              "owasp": ["A02"],
              "asvs": ["1.1.1"],
              "cwe": ["CWE-798"],
              "internal_policy_refs": ["IP123"],
              "applicability_rationale": "Rationale",
              "requirement_ids": ["REQ-1"]
            }
          ],
          "checks": [
            {
              "check_id": "CHECK-001",
              "control_id": "SEC-001",
              "test_type": "positive",
              "scenario": "Scenario",
              "pass_criteria": "Criteria",
              "evidence_ref": "EV-1"
            }
          ],
          "evidence_artifacts": [
            {
              "artifact_id": "ART-001",
              "type": "policy",
              "location": "docs/policy.md",
              "description": "Policy"
            }
          ],
          "review_signoff": {
            "reviewer": "Lead",
            "role": "security lead",
            "decision": "approved",
            "reference": "REF-1",
            "date": "2026-06-09"
          }
        }
        """;

    private const string SampleSdpEvidenceJson = """
        {
          "project_code": "ACME",
          "generated_at": "2026-06-09T00:00:00Z",
          "controls": [
            {
              "control_id": "SDP-001",
              "name": "Code review",
              "status": "pass",
              "evidence_type": "policy_doc",
              "evidence_ref": "docs/review-policy.md",
              "owner": "Engineering",
              "last_reviewed": "2026-06-09",
              "notes": "Reviewed",
              "mapped_requirements": ["REQ-1"]
            }
          ]
        }
        """;

    private readonly TestWebApplicationFactory _factory;

    public SecurityReviewReportApiTests()
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
            """{"code":"SEC","name":"Security Test","description":"Test","timeSheetCode":"TS-001","complianceDomain":"ClinicalUk"}""",
            Encoding.UTF8,
            "application/json");
        var response = await client.PostAsync("/api/v1/projects", content);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("data").GetProperty("id").GetString()!;
    }

    private static async Task SeedSecurityArtefactsAsync(HttpClient client, string projectId)
    {
        var payload = JsonSerializer.Serialize(new
        {
            artefacts = new[]
            {
                new
                {
                    filePath = "output/SECURITY_ASSURANCE_DATA.json",
                    contentType = "application/json",
                    content = SampleSecurityAssuranceJson
                },
                new
                {
                    filePath = "output/SDP_EVIDENCE.json",
                    contentType = "application/json",
                    content = SampleSdpEvidenceJson
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
            $"/api/v1/projects/{Guid.NewGuid()}/security-review-report",
            content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Generate_SourceArtefactsMissing_Returns409Conflict()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);

        var response = await client.PostAsync(
            $"/api/v1/projects/{projectId}/security-review-report",
            content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Generate_SourceArtefactsExist_ReturnsSpreadsheetContentType()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);
        await SeedSecurityArtefactsAsync(client, projectId);

        var response = await client.PostAsync(
            $"/api/v1/projects/{projectId}/security-review-report",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(SpreadsheetContentType, response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
      public async Task Generate_SourceArtefactsExist_ReturnsValidWorkbookWithExpectedSheets()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);
        await SeedSecurityArtefactsAsync(client, projectId);

        var response = await client.PostAsync(
            $"/api/v1/projects/{projectId}/security-review-report",
            content: null);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);

        Assert.NotEmpty(bytes);
        Assert.NotNull(workbook.Worksheet("Summary"));
        Assert.NotNull(workbook.Worksheet("Attack Vector Coverage"));
        Assert.NotNull(workbook.Worksheet("Control Mappings"));
        Assert.NotNull(workbook.Worksheet("Security Checks"));
        Assert.NotNull(workbook.Worksheet("Evidence Artifacts"));
        Assert.NotNull(workbook.Worksheet("SDP Evidence"));
        Assert.NotNull(workbook.Worksheet("Gaps-Blockers"));
    }

    [Fact]
    public async Task Generate_SourceArtefactsExist_ReturnsAttachmentFilename()
    {
        var client = _factory.CreateAdminClient();
        var projectId = await CreateProjectAsync(client);
        await SeedSecurityArtefactsAsync(client, projectId);

        var response = await client.PostAsync(
            $"/api/v1/projects/{projectId}/security-review-report",
            content: null);

        var disposition = response.Content.Headers.ContentDisposition;

        Assert.NotNull(disposition);
        Assert.Contains(".xlsx", disposition!.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}