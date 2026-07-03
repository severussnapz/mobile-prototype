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
/// <param name="CurrentHtml">
/// The full current prototype document. The server locates the selected element within this
/// document (fingerprint match) and performs the element replacement server-side, so the client
/// never has to string-replace browser-serialised <c>outerHTML</c> against the raw source HTML.
/// </param>
public sealed record PrototypeElementEditRequest(
    string SelectedOuterHtml,
    string Instruction,
    string ActiveUiKit,
    string CurrentHtml);
