namespace Genesis.AI.Domain.Queries.GetPrototypeDemoHtml;

/// <summary>
/// Result of a <see cref="GetPrototypeDemoHtmlQuery"/>. On success carries the stored
/// HTML document. On failure carries no content.
/// </summary>
public sealed record GetPrototypeDemoHtmlResult(
    GetPrototypeDemoHtmlStatus Status,
    string Html)
{
    public static GetPrototypeDemoHtmlResult NotFound()
    {
        return new GetPrototypeDemoHtmlResult(GetPrototypeDemoHtmlStatus.NotFound, string.Empty);
    }

    public static GetPrototypeDemoHtmlResult Succeeded(string html)
    {
        return new GetPrototypeDemoHtmlResult(GetPrototypeDemoHtmlStatus.Success, html);
    }
}
