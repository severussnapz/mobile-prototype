namespace Genesis.AI.Api.Features.Conversations;

public sealed class ImageAttachment
{
    /// <summary>Base64-encoded image data.</summary>
    public string Data { get; init; } = null!;

    /// <summary>MIME type (e.g. image/png, image/jpeg, image/gif, image/webp).</summary>
    public string MediaType { get; init; } = null!;
}
