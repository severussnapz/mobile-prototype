using System.Text.Json;
using Genesis.AI.Domain.SecurityReviewReport;

namespace Genesis.AI.Infrastructure.Services;

/// <summary>
/// Builds the security review report workbook from structured security JSON.
/// </summary>
public sealed class SecurityReviewReportBuilder : ISecurityReviewReportBuilder
{
    public byte[] Build(string securityAssuranceJson, string sdpEvidenceJson)
    {
        if (string.IsNullOrWhiteSpace(securityAssuranceJson))
            throw new InvalidOperationException("Security assurance JSON payload is empty.");

        if (string.IsNullOrWhiteSpace(sdpEvidenceJson))
            throw new InvalidOperationException("SDP evidence JSON payload is empty.");

        using var securityAssuranceDocument = JsonDocument.Parse(securityAssuranceJson);
        using var sdpEvidenceDocument = JsonDocument.Parse(sdpEvidenceJson);

        SecurityReviewJsonValidator.ValidateSecurityAssurance(securityAssuranceDocument.RootElement);
        SecurityReviewJsonValidator.ValidateSdpEvidence(sdpEvidenceDocument.RootElement);

        return SecurityReviewWorkbookWriter.CreateWorkbook(
            securityAssuranceDocument.RootElement,
            sdpEvidenceDocument.RootElement);
    }
}
