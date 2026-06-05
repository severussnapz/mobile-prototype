namespace Genesis.AI.Domain.Commands.GenerateHazardLog;

/// <summary>
/// Result of a <see cref="GenerateHazardLogCommand"/>. On success carries the
/// spreadsheet bytes, the download file name, the persisted artefact identifier,
/// and the number of hazards rendered. On failure carries the reason only.
/// </summary>
public sealed record GenerateHazardLogResult(
    GenerateHazardLogStatus Status,
    byte[] Content,
    string FileName,
    Guid? ArtefactId,
    int HazardCount,
    string? ErrorDetail)
{
    public static GenerateHazardLogResult Failure(GenerateHazardLogStatus status, string errorDetail)
    {
        return new GenerateHazardLogResult(status, [], string.Empty, null, 0, errorDetail);
    }

    public static GenerateHazardLogResult Succeeded(byte[] content, string fileName, Guid artefactId, int hazardCount)
    {
        return new GenerateHazardLogResult(
            GenerateHazardLogStatus.Success, content, fileName, artefactId, hazardCount, null);
    }
}
