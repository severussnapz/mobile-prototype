using Genesis.AI.Infrastructure.Services;

namespace Genesis.AI.Tests.Services;

public class Pr1625DpiaDocxBuilderTests
{
    private const string ValidDpiaJson = """
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

    private readonly Pr1625DpiaDocxBuilder _builder = new();

    [Fact]
    public void Build_WithValidPayload_ReturnsNonEmptyDocxBytes()
    {
        var bytes = _builder.Build(ValidDpiaJson);

        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void Build_WithValidPayload_ReturnsZipFormattedDocument()
    {
        var bytes = _builder.Build(ValidDpiaJson);

        // .docx files are ZIP archives — verify the PK signature.
        Assert.Equal(0x50, bytes[0]);
        Assert.Equal(0x4B, bytes[1]);
    }

    [Fact]
    public void Build_WithEmptyPayload_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => _builder.Build(string.Empty));
    }

    [Fact]
    public void Build_WithBlankRequiredField_ThrowsInvalidOperationException()
    {
        var blankTitleJson = ValidDpiaJson.Replace(
            "\"title\": \"Patient Access\"", "\"title\": \"\"", StringComparison.Ordinal);

        Assert.Throws<InvalidOperationException>(() => _builder.Build(blankTitleJson));
    }
}
