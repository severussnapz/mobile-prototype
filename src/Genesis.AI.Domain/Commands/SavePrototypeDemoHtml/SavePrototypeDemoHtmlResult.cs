namespace Genesis.AI.Domain.Commands.SavePrototypeDemoHtml;

/// <summary>
/// Result of a <see cref="SavePrototypeDemoHtmlCommand"/>. On success carries the
/// persisted artefact's identifier. On failure carries the reason only.
/// </summary>
public sealed record SavePrototypeDemoHtmlResult(
    SavePrototypeDemoHtmlStatus Status,
    Guid ArtefactId,
    string? ErrorDetail)
{
    public static SavePrototypeDemoHtmlResult Failure(SavePrototypeDemoHtmlStatus status, string errorDetail)
    {
        return new SavePrototypeDemoHtmlResult(status, Guid.Empty, errorDetail);
    }

    public static SavePrototypeDemoHtmlResult Succeeded(Guid artefactId)
    {
        return new SavePrototypeDemoHtmlResult(SavePrototypeDemoHtmlStatus.Success, artefactId, null);
    }
}
