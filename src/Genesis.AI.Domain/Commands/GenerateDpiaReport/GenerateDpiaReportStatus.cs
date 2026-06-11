namespace Genesis.AI.Domain.Commands.GenerateDpiaReport;

/// <summary>
/// Outcome of a DPIA report generation request.
/// </summary>
public enum GenerateDpiaReportStatus
{
    /// <summary>The DPIA report was generated and persisted successfully.</summary>
    Success,

    /// <summary>No project exists with the requested identifier.</summary>
    ProjectNotFound,

    /// <summary>The project has no DPIA JSON source artefact.</summary>
    DataNotFound,

    /// <summary>The DPIA JSON source was invalid for report generation.</summary>
    DataInvalid
}
