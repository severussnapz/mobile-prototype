namespace Genesis.AI.Domain.SecurityReviewReport;

/// <summary>
/// Builds the on-demand security review report from structured security source data.
/// </summary>
public interface ISecurityReviewReportBuilder
{
    /// <summary>
    /// Produces a security review workbook (.xlsx) as binary bytes.
    /// </summary>
    /// <param name="securityAssuranceJson">The source JSON payload from output/SECURITY_ASSURANCE_DATA.json.</param>
    /// <param name="sdpEvidenceJson">The source JSON payload from output/SDP_EVIDENCE.json.</param>
    byte[] Build(string securityAssuranceJson, string sdpEvidenceJson);
}