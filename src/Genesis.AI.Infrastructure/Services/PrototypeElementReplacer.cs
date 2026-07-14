using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;

namespace Genesis.AI.Infrastructure.Services;

/// <summary>
/// Locates a selected element inside a full prototype document and replaces it with an
/// updated element, returning the serialised document.
///
/// The element is located by a <b>serialisation-independent fingerprint</b> — local tag
/// name + attribute name/value map + normalised text content — rather than by string
/// equality. This is deliberate: the selection bridge sends the browser-serialised
/// <c>outerHTML</c>, which differs from the source markup (self-closing <c>/&gt;</c> vs an
/// explicit <c>&lt;/rect&gt;</c> close tag, attribute quoting, whitespace). A raw string
/// match against the source therefore fails; a fingerprint match does not.
/// </summary>
internal static class PrototypeElementReplacer
{
    /// <summary>
    /// Returns the full document with the first element matching <paramref name="selectedOuterHtml"/>
    /// replaced by <paramref name="updatedOuterHtml"/>, or <c>null</c> when no element matches
    /// (stale document, wrong project, unparsable selection).
    /// </summary>
    // ponytail: first fingerprint match wins. Two structurally identical elements (e.g. two
    // <rect> with the same attributes and text) are indistinguishable here — parity with the
    // previous client-side string.replace, which also replaced the first occurrence.
    // Upgrade path: have the selection bridge send a positional index or a data-genesis-id.
    internal static async Task<string?> ReplaceElementAsync(
        string currentHtml,
        string selectedOuterHtml,
        string updatedOuterHtml,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(currentHtml) || string.IsNullOrWhiteSpace(selectedOuterHtml))
        {
            return null;
        }

        var context = BrowsingContext.New(AngleSharp.Configuration.Default);

        var selectedFragment = await context.OpenAsync(
            response => response.Content(selectedOuterHtml), cancellationToken);
        var selectedElement = selectedFragment.Body?.Children.FirstOrDefault();
        if (selectedElement is null)
        {
            return null;
        }

        var document = await context.OpenAsync(
            response => response.Content(currentHtml), cancellationToken);

        var match = document.All.FirstOrDefault(
            candidate => FingerprintMatches(selectedElement, candidate));
        if (match is null)
        {
            return null;
        }

        // Setting OuterHtml reparses the replacement in the matched element's own context
        // (HTML vs SVG namespace), so a self-closing SVG child round-trips correctly.
        match.OuterHtml = updatedOuterHtml;

        // ponytail: AngleSharp renormalises the entire document on serialisation (quoting,
        // self-closing, whitespace of untouched nodes). This is a cosmetic diff on markup the
        // edit did not touch; acceptable for a preview document. Upgrade path: a targeted
        // string splice if byte-for-byte preservation of untouched markup ever matters.
        return PrototypeDomMutationHelper.SerializeDocument(document, currentHtml);
    }

    private static bool FingerprintMatches(IElement selected, IElement candidate)
    {
        if (!string.Equals(selected.LocalName, candidate.LocalName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (selected.Attributes.Length != candidate.Attributes.Length)
        {
            return false;
        }

        var candidateAttributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var attribute in candidate.Attributes)
        {
            candidateAttributes[attribute.Name] = attribute.Value;
        }

        foreach (var attribute in selected.Attributes)
        {
            if (!candidateAttributes.TryGetValue(attribute.Name, out var value)
                || !string.Equals(value, attribute.Value, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return string.Equals(
            NormaliseWhitespace(selected.TextContent),
            NormaliseWhitespace(candidate.TextContent),
            StringComparison.Ordinal);
    }

    private static string NormaliseWhitespace(string value)
    {
        return Regex.Replace(value, @"\s+", " ").Trim();
    }
}
