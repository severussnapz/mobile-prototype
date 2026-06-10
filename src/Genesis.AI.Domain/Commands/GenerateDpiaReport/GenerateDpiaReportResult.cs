namespace Genesis.AI.Domain.Commands.GenerateDpiaReport;

/// <summary>
/// Result of a <see cref="GenerateDpiaReportCommand"/>.
/// </summary>
public sealed record GenerateDpiaReportResult(
    GenerateDpiaReportStatus Status,
    byte[] Content,
    string FileName,
    Guid? ArtefactId,
    string? ErrorDetail)
{
    public static GenerateDpiaReportResult Failure(GenerateDpiaReportStatus status, string errorDetail)
    {
        return new GenerateDpiaReportResult(status, [], string.Empty, null, errorDetail);
    }

    public static GenerateDpiaReportResult Succeeded(byte[] content, string fileName, Guid artefactId)
    {
        return new GenerateDpiaReportResult(GenerateDpiaReportStatus.Success, content, fileName, artefactId, null);
    }
}
