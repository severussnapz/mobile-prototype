namespace Genesis.AI.Domain.Interfaces;

/// <summary>Input contract for a targeted single-element edit.</summary>
/// <param name="SelectedOuterHtml">
/// The exact <c>outerHTML</c> of the element the user selected (postMessage bridge, Decision A).
/// </param>
/// <param name="Instruction">Natural-language instruction describing what to change.</param>
/// <param name="ActiveUiKit">
/// The UI kit active in the current prototype (e.g. <c>emis-x</c>, <c>bootstrap</c>, <c>none</c>).
/// Injected into the prompt so the model constrains generated classes to the active vocabulary.
/// </param>
public sealed record PrototypeElementEditRequest(
    string SelectedOuterHtml,
    string Instruction,
    string ActiveUiKit);
