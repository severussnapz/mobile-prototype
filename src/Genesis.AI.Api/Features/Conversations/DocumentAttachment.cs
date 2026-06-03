namespace Genesis.AI.Api.Features.Conversations;

public sealed class DocumentAttachment
{
    /// <summary>Base64-encoded document data.</summary>
    public string Data { get; init; } = null!;

    /// <summary>MIME type (e.g. application/pdf, text/plain, text/html).</summary>
    public string MediaType { get; init; } = null!;

    /// <summary>Original filename of the document.</summary>
    public string FileName { get; init; } = null!;
}
