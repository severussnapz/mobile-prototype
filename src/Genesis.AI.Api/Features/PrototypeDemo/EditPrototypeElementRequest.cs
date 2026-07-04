namespace Genesis.AI.Api.Features.PrototypeDemo;

/// <summary>
/// HTTP request body for the targeted single-element edit endpoint.
/// Deserialised from camelCase JSON and mapped to the domain record
/// <see cref="Genesis.AI.Domain.Interfaces.PrototypeElementEditRequest"/>.
/// </summary>
public sealed class EditPrototypeElementRequest
{
    public string SelectedOuterHtml { get; init; } = string.Empty;
    public string Instruction { get; init; } = string.Empty;
    public string ActiveUiKit { get; init; } = string.Empty;

    /// <summary>
    /// The full current prototype document. The server locates the selected element within this
    /// document and replaces it server-side, avoiding a client-side serialisation mismatch.
    /// </summary>
    public string CurrentHtml { get; init; } = string.Empty;

    public Guid? ConversationId { get; init; }
}
