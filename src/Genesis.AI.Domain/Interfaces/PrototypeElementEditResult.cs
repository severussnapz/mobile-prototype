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
