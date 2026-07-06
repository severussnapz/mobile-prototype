namespace Genesis.AI.Api.Features.Conversations;

public sealed class StreamMessageRequest
{
    public string Content { get; init; } = null!;

    /// <summary>
    /// Optional browser-provided preview blob URL for diagnostics.
    /// The API does not fetch this URL directly; it is browser-local.
    /// </summary>
    public string? PreviewBlobUrl { get; init; }

    /// <summary>
    /// Optional browser-provided preview HTML content.
    /// Used as a fallback only when stored prototype/index.html appears stub-like.
    /// </summary>
    public string? PreviewHtml { get; init; }

    /// <summary>
    /// When true, re-runs the AI on existing conversation history without adding a new user message.
    /// Used when the previous AI response failed mid-stream and the user wants to retry.
    /// </summary>
    public bool Retry { get; init; }

    /// <summary>
    /// Optional image attachments to include with the message.
    /// Each image must have base64-encoded data and a media type (image/png, image/jpeg, image/gif, image/webp).
    /// </summary>
    public List<ImageAttachment>? Images { get; init; }

    /// <summary>
    /// Optional document attachments to include with the message.
    /// Supported formats: pdf, csv, doc, docx, xls, xlsx, html, txt, md.
    /// </summary>
    public List<DocumentAttachment>? Documents { get; init; }
}
