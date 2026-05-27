namespace Genesis.AI.Api.Dtos;

public sealed class StreamMessageRequest
{
    public string Content { get; init; } = null!;

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
