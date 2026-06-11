namespace Genesis.AI.Domain.Commands.GenerateSecurityReviewReport;

/// <summary>
/// Result of a <see cref="GenerateSecurityReviewReportCommand"/>.
/// </summary>
public sealed record GenerateSecurityReviewReportResult(
    GenerateSecurityReviewReportStatus Status,
    byte[] Content,
    string FileName,
    Guid? ArtefactId,
    string? ErrorDetail)
{
    public static GenerateSecurityReviewReportResult Failure(
        GenerateSecurityReviewReportStatus status,
        string errorDetail)
    {
        return new GenerateSecurityReviewReportResult(status, [], string.Empty, null, errorDetail);
    }

    public static GenerateSecurityReviewReportResult Succeeded(byte[] content, string fileName, Guid artefactId)
    {
        return new GenerateSecurityReviewReportResult(GenerateSecurityReviewReportStatus.Success, content, fileName, artefactId, null);
    }
}