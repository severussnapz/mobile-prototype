namespace Genesis.AI.Domain.Interfaces;

public enum PrototypeElementEditStatus
{
    /// <summary>The model returned a valid updated element and all deterministic checks passed.</summary>
    Applied,

    /// <summary>
    /// The model indicated the instruction cannot be satisfied by editing this element alone
    /// (EDIT_OUT_OF_SCOPE marker was present). The element is returned unchanged.
    /// </summary>
    OutOfScope,

    /// <summary>
    /// The instruction was ambiguous; the model returned the element unchanged with a
    /// EDIT_NEEDS_CLARIFICATION marker rather than guessing.
    /// </summary>
    NeedsClarification,

    /// <summary>
    /// The deterministic post-generation checks rejected the model's response (e.g. prose wrapping,
    /// dropped attributes, untargeted child mutation, or unrequested class addition).
    /// </summary>
    Rejected
}
