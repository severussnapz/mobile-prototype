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
}
