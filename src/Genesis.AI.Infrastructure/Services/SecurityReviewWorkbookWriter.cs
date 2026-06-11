using System.Globalization;
using System.Text.Json;
using ClosedXML.Excel;

namespace Genesis.AI.Infrastructure.Services;

/// <summary>
/// Writes the security review workbook sheets from validated security assurance
/// and SDP evidence JSON payloads.
/// </summary>
internal static class SecurityReviewWorkbookWriter
{
    private static readonly string[] SummaryHeaders = ["Field", "Value"];
    private static readonly string[] AttackVectorHeaders = ["Vector", "Status", "Controls", "Evidence Refs"];
    private static readonly string[] ControlMappingHeaders =
        ["Control ID", "Title", "OWASP", "ASVS", "CWE", "Internal Policy Refs", "Requirement IDs", "Rationale"];
    private static readonly string[] SecurityCheckHeaders =
        ["Check ID", "Control", "Type", "Scenario", "Pass Criteria", "Evidence Ref", "Requirement"];
    private static readonly string[] EvidenceArtifactHeaders = ["Artifact ID", "Type", "Location", "Description"];
    private static readonly string[] SdpEvidenceHeaders =
        ["Control ID", "Name", "Status", "Evidence Type", "Evidence Ref", "Owner", "Last Reviewed", "Notes", "Mapped Requirements"];
    private static readonly string[] GapHeaders = ["Gap ID", "Source", "Status", "Evidence", "Recommended Action"];
    private const string GapsSheetName = "Gaps-Blockers";

    public static byte[] CreateWorkbook(JsonElement securityAssurance, JsonElement sdpEvidence)
    {
        using var workbook = new XLWorkbook();

        AppendSummarySheet(workbook, securityAssurance, sdpEvidence);
        AppendAttackVectorCoverageSheet(workbook, securityAssurance);
        AppendControlMappingsSheet(workbook, securityAssurance);
        AppendSecurityChecksSheet(workbook, securityAssurance);
        AppendEvidenceArtifactsSheet(workbook, securityAssurance);
        AppendSdpEvidenceSheet(workbook, sdpEvidence);
        AppendGapsSheet(workbook, securityAssurance, sdpEvidence);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void AppendSummarySheet(XLWorkbook workbook, JsonElement securityAssurance, JsonElement sdpEvidence)
    {
        var worksheet = workbook.Worksheets.Add("Summary");
        var generatedAt = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);
        var project = securityAssurance.GetProperty("project");
        var threatModel = securityAssurance.GetProperty("threat_model");
        var signOff = securityAssurance.GetProperty("review_signoff");

        var rows = new List<string[]>
        {
            new[] { "Generated", generatedAt },
            new[] { "Document version", SecurityReviewJson.GetRequiredString(securityAssurance, "document_version") },
            new[] { "Project code", SecurityReviewJson.GetRequiredString(sdpEvidence, "project_code") },
            new[] { "Project", SecurityReviewJson.GetRequiredString(project, "name") },
            new[] { "Summary", SecurityReviewJson.GetRequiredString(project, "summary") },
            new[] { "Architecture context", SecurityReviewJson.GetRequiredString(project, "architecture_context") },
            new[] { "Data sensitivity", SecurityReviewJson.JoinOptionalArray(project, "data_sensitivity") },
            new[] { "Assets", SecurityReviewJson.JoinArray(threatModel.GetProperty("assets")) },
            new[] { "Actors", SecurityReviewJson.JoinArray(threatModel.GetProperty("actors")) },
            new[] { "Entry points", SecurityReviewJson.JoinArray(threatModel.GetProperty("entry_points")) },
            new[] { "Abuse cases", SecurityReviewJson.JoinArray(threatModel.GetProperty("abuse_cases")) },
            new[] { "Reviewer", SecurityReviewJson.GetRequiredString(signOff, "reviewer") },
            new[] { "Role", SecurityReviewJson.GetRequiredString(signOff, "role") },
            new[] { "Decision", SecurityReviewJson.GetRequiredString(signOff, "decision") },
            new[] { "Reference", SecurityReviewJson.GetRequiredString(signOff, "reference") },
            new[] { "Date", SecurityReviewJson.GetRequiredString(signOff, "date") }
        };

        WriteTable(worksheet, SummaryHeaders, rows);
    }

    private static void AppendAttackVectorCoverageSheet(XLWorkbook workbook, JsonElement securityAssurance)
    {
        var worksheet = workbook.Worksheets.Add("Attack Vector Coverage");
        var attackVectors = securityAssurance.GetProperty("attack_vector_coverage");

        var rows = new List<string[]>
        {
            CreateAttackVectorRow("repo_secrets", attackVectors.GetProperty("repo_secrets")),
            CreateAttackVectorRow("ci_cd_exposure", attackVectors.GetProperty("ci_cd_exposure")),
            CreateAttackVectorRow("supply_chain", attackVectors.GetProperty("supply_chain")),
            CreateAttackVectorRow("injection", attackVectors.GetProperty("injection")),
            CreateAttackVectorRow("authn_authz", attackVectors.GetProperty("authn_authz")),
            CreateAttackVectorRow("crypto", attackVectors.GetProperty("crypto")),
            CreateAttackVectorRow("logging_monitoring", attackVectors.GetProperty("logging_monitoring"))
        };

        WriteTable(worksheet, AttackVectorHeaders, rows);
    }

    private static void AppendControlMappingsSheet(XLWorkbook workbook, JsonElement securityAssurance)
    {
        var worksheet = workbook.Worksheets.Add("Control Mappings");

        var rows = securityAssurance
            .GetProperty("control_mappings")
            .EnumerateArray()
            .Select(mapping => new[]
            {
                SecurityReviewJson.GetRequiredString(mapping, "control_id"),
                SecurityReviewJson.GetRequiredString(mapping, "title"),
                SecurityReviewJson.JoinArray(mapping.GetProperty("owasp")),
                SecurityReviewJson.JoinOptionalArray(mapping, "asvs"),
                SecurityReviewJson.JoinOptionalArray(mapping, "cwe"),
                SecurityReviewJson.JoinArray(mapping.GetProperty("internal_policy_refs")),
                SecurityReviewJson.JoinArray(mapping.GetProperty("requirement_ids")),
                SecurityReviewJson.GetRequiredString(mapping, "applicability_rationale")
            })
            .ToList();

        WriteTable(worksheet, ControlMappingHeaders, rows);
    }

    private static void AppendSecurityChecksSheet(XLWorkbook workbook, JsonElement securityAssurance)
    {
        var worksheet = workbook.Worksheets.Add("Security Checks");

        var rows = securityAssurance
            .GetProperty("checks")
            .EnumerateArray()
            .Select(check => new[]
            {
                SecurityReviewJson.GetRequiredString(check, "check_id"),
                SecurityReviewJson.GetRequiredString(check, "control_id"),
                SecurityReviewJson.GetRequiredString(check, "test_type"),
                SecurityReviewJson.GetRequiredString(check, "scenario"),
                SecurityReviewJson.GetRequiredString(check, "pass_criteria"),
                SecurityReviewJson.GetRequiredString(check, "evidence_ref"),
                SecurityReviewJson.OptString(check, "requirement_id")
            })
            .ToList();

        WriteTable(worksheet, SecurityCheckHeaders, rows);
    }

    private static void AppendEvidenceArtifactsSheet(XLWorkbook workbook, JsonElement securityAssurance)
    {
        var worksheet = workbook.Worksheets.Add("Evidence Artifacts");

        var rows = securityAssurance
            .GetProperty("evidence_artifacts")
            .EnumerateArray()
            .Select(artifact => new[]
            {
                SecurityReviewJson.GetRequiredString(artifact, "artifact_id"),
                SecurityReviewJson.GetRequiredString(artifact, "type"),
                SecurityReviewJson.GetRequiredString(artifact, "location"),
                SecurityReviewJson.GetRequiredString(artifact, "description")
            })
            .ToList();

        WriteTable(worksheet, EvidenceArtifactHeaders, rows);
    }

    private static void AppendSdpEvidenceSheet(XLWorkbook workbook, JsonElement sdpEvidence)
    {
        var worksheet = workbook.Worksheets.Add("SDP Evidence");

        var rows = sdpEvidence
            .GetProperty("controls")
            .EnumerateArray()
            .Select(control => new[]
            {
                SecurityReviewJson.GetRequiredString(control, "control_id"),
                SecurityReviewJson.GetRequiredString(control, "name"),
                SecurityReviewJson.GetRequiredString(control, "status"),
                SecurityReviewJson.GetRequiredString(control, "evidence_type"),
                SecurityReviewJson.GetRequiredString(control, "evidence_ref"),
                SecurityReviewJson.GetRequiredString(control, "owner"),
                SecurityReviewJson.GetRequiredString(control, "last_reviewed"),
                SecurityReviewJson.OptString(control, "notes"),
                SecurityReviewJson.JoinOptionalArray(control, "mapped_requirements")
            })
            .ToList();

        WriteTable(worksheet, SdpEvidenceHeaders, rows);
    }

    private static void AppendGapsSheet(XLWorkbook workbook, JsonElement securityAssurance, JsonElement sdpEvidence)
    {
        var worksheet = workbook.Worksheets.Add(GapsSheetName);
        var rows = new List<string[]>();
        var gapIndex = 1;

        gapIndex = AppendAttackVectorGaps(securityAssurance, rows, gapIndex);
        AppendSdpControlGaps(sdpEvidence, rows, gapIndex);

        if (rows.Count == 0)
        {
            rows.Add(["GAP-000", "none", "none", string.Empty, "No open gaps identified."]);
        }

        WriteTable(worksheet, GapHeaders, rows);
    }

    private static int AppendAttackVectorGaps(JsonElement securityAssurance, List<string[]> rows, int gapIndex)
    {
        foreach (var vector in securityAssurance.GetProperty("attack_vector_coverage").EnumerateObject())
        {
            var status = SecurityReviewJson.GetRequiredString(vector.Value, "status");
            if (!string.Equals(status, "partial", StringComparison.Ordinal) &&
                !string.Equals(status, "gap", StringComparison.Ordinal))
            {
                continue;
            }

            rows.Add([
                $"GAP-{gapIndex:000}",
                $"attack_vector:{vector.Name}",
                status,
                SecurityReviewJson.JoinArray(vector.Value.GetProperty("evidence_refs")),
                "Review control coverage and add missing evidence or residual-risk rationale."
            ]);
            gapIndex++;
        }

        return gapIndex;
    }

    private static void AppendSdpControlGaps(JsonElement sdpEvidence, List<string[]> rows, int gapIndex)
    {
        foreach (var control in sdpEvidence.GetProperty("controls").EnumerateArray())
        {
            var status = SecurityReviewJson.GetRequiredString(control, "status");
            if (!string.Equals(status, "partial", StringComparison.Ordinal) &&
                !string.Equals(status, "fail", StringComparison.Ordinal))
            {
                continue;
            }

            rows.Add([
                $"GAP-{gapIndex:000}",
                $"sdp_control:{SecurityReviewJson.GetRequiredString(control, "control_id")}",
                status,
                SecurityReviewJson.GetRequiredString(control, "evidence_ref"),
                "Resolve the control status before the next security sign-off."
            ]);
            gapIndex++;
        }
    }

    private static string[] CreateAttackVectorRow(string vectorName, JsonElement vector)
    {
        return
        [
            vectorName,
            SecurityReviewJson.GetRequiredString(vector, "status"),
            SecurityReviewJson.JoinArray(vector.GetProperty("controls")),
            SecurityReviewJson.JoinArray(vector.GetProperty("evidence_refs"))
        ];
    }

    private static void WriteTable(IXLWorksheet worksheet, string[] headers, List<string[]> rows)
    {
        for (var column = 0; column < headers.Length; column++)
        {
            worksheet.Cell(1, column + 1).Value = headers[column];
        }

        var headerRange = worksheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            for (var column = 0; column < headers.Length; column++)
            {
                var value = column < row.Length ? row[column] : string.Empty;
                worksheet.Cell(rowIndex + 2, column + 1).Value = value;
            }
        }

        var lastRow = Math.Max(2, rows.Count + 1);
        var usedRange = worksheet.Range(1, 1, lastRow, headers.Length);
        usedRange.Style.Alignment.WrapText = true;
        usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        worksheet.SheetView.FreezeRows(1);
        usedRange.SetAutoFilter();
        worksheet.Columns().AdjustToContents();
    }
}
