using Genesis.AI.Infrastructure.Services;

namespace Genesis.AI.Tests.Services;

public class SecurityReviewReportBuilderTests
{
    private const string ValidSecurityAssuranceJson = """
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

    private const string ValidSdpEvidenceJson = """
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

    private readonly SecurityReviewReportBuilder _builder = new();

    [Fact]
    public void Build_WithValidPayloads_ReturnsNonEmptyWorkbookBytes()
    {
        var bytes = _builder.Build(ValidSecurityAssuranceJson, ValidSdpEvidenceJson);

        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void Build_WithValidPayloads_ReturnsZipFormattedWorkbook()
    {
        var bytes = _builder.Build(ValidSecurityAssuranceJson, ValidSdpEvidenceJson);

        // .xlsx files are ZIP archives — verify the PK signature.
        Assert.Equal(0x50, bytes[0]);
        Assert.Equal(0x4B, bytes[1]);
    }

    [Fact]
    public void Build_WithEmptySecurityAssuranceJson_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(
            () => _builder.Build(string.Empty, ValidSdpEvidenceJson));
    }

    [Fact]
    public void Build_WithEmptySdpEvidenceJson_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(
            () => _builder.Build(ValidSecurityAssuranceJson, string.Empty));
    }
}
