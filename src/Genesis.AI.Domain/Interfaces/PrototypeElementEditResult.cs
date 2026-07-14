namespace Genesis.AI.Domain.Interfaces;

/// <summary>Outcome of a targeted single-element edit.</summary>
public sealed record PrototypeElementEditResult
{
    public PrototypeElementEditStatus Status { get; init; }

    /// <summary>
    /// The (potentially updated) element HTML. For <see cref="PrototypeElementEditStatus.Applied"/>
    /// this is the new element. For all other statuses it is the original element unchanged (which
    /// may carry an inline marker comment from the model).
    /// </summary>
    public string UpdatedOuterHtml { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable explanation for non-Applied outcomes. <c>null</c> when status is Applied.
    /// </summary>
    public string? RejectionReason { get; init; }

    /// <summary>
    /// The full prototype document with the selected element replaced. Populated only when
    /// <see cref="Status"/> is <see cref="PrototypeElementEditStatus.Applied"/> and the element was
    /// located in the supplied CurrentHtml; <c>null</c> for every other status. The client renders
    /// this directly, avoiding a browser-vs-source serialisation mismatch on string replacement.
    /// </summary>
    public string? UpdatedFullHtml { get; init; }

    public static PrototypeElementEditResult Applied(string updatedOuterHtml)
    {
        return new PrototypeElementEditResult
        {
            Status = PrototypeElementEditStatus.Applied,
            UpdatedOuterHtml = updatedOuterHtml
        };
    }

    public static PrototypeElementEditResult OutOfScope(string originalOuterHtml, string reason)
    {
        return new PrototypeElementEditResult
        {
            Status = PrototypeElementEditStatus.OutOfScope,
            UpdatedOuterHtml = originalOuterHtml,
            RejectionReason = reason
        };
    }

    public static PrototypeElementEditResult NeedsClarification(string originalOuterHtml, string reason)
    {
        return new PrototypeElementEditResult
        {
            Status = PrototypeElementEditStatus.NeedsClarification,
            UpdatedOuterHtml = originalOuterHtml,
            RejectionReason = reason
        };
    }

    public static PrototypeElementEditResult Rejected(string reason)
    {
        return new PrototypeElementEditResult
        {
            Status = PrototypeElementEditStatus.Rejected,
            RejectionReason = reason
        };
    }
}
