namespace Genesis.AI.Domain.Commands.GenerateSecurityReviewReport;

/// <summary>
/// Outcome of a security review report generation request.
/// </summary>
public enum GenerateSecurityReviewReportStatus
{
    /// <summary>The security review report was generated and persisted successfully.</summary>
    Success,

    /// <summary>No project exists with the requested identifier.</summary>
    ProjectNotFound,

    /// <summary>The project has no security assurance JSON source artefact.</summary>
    SecurityAssuranceDataNotFound,

    /// <summary>The project has no SDP evidence JSON source artefact.</summary>
    SdpEvidenceNotFound,

    /// <summary>The security source artefacts were invalid for report generation.</summary>
    DataInvalid
}