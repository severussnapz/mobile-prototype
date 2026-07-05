using System.Text.RegularExpressions;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

/// <summary>
/// Deterministic post-generation validation for targeted prototype element edits.
/// Validates model output against six failure modes (failure modes 1–6) and returns
/// a four-valued result: Applied, OutOfScope, NeedsClarification, or Rejected.
/// </summary>
internal sealed class PrototypeElementValidator
{
    private const string OutOfScopeMarker = "EDIT_OUT_OF_SCOPE";
    private const string ClarificationMarker = "EDIT_NEEDS_CLARIFICATION";
    private static readonly char[] SplitChars = { ' ', '\t', '\n' };

    public static PrototypeElementEditResult Validate(string modelOutput, string originalOuterHtml)
    {
        // Mode 3: EDIT_OUT_OF_SCOPE — check before the prose-check (IsSingleRootElement) because
        // the model is expected to prepend a single marker comment and nothing else. However, the
        // response must still be well-formed: trailing prose after the marker+element is a contract
        // violation and is rejected so the ordering is meaningful, not a blanket bypass.
        if (modelOutput.Contains(OutOfScopeMarker, StringComparison.Ordinal))
        {
            var elementAfterScope = ExtractElementAfterMarker(modelOutput, originalOuterHtml);
            if (!IsSingleRootElement(elementAfterScope))
            {
                return PrototypeElementEditResult.Rejected(
                    "EDIT_OUT_OF_SCOPE response contains trailing prose after the element.");
            }

            return PrototypeElementEditResult.OutOfScope(
                elementAfterScope,
                reason: ExtractMarkerReason(modelOutput, OutOfScopeMarker));
        }

        // Mode 6: EDIT_NEEDS_CLARIFICATION — same pattern, same well-formedness gate.
        if (modelOutput.Contains(ClarificationMarker, StringComparison.Ordinal))
        {
            var elementAfterClarification = ExtractElementAfterMarker(modelOutput, originalOuterHtml);
            if (!IsSingleRootElement(elementAfterClarification))
            {
                return PrototypeElementEditResult.Rejected(
                    "EDIT_NEEDS_CLARIFICATION response contains trailing prose after the element.");
            }

            return PrototypeElementEditResult.NeedsClarification(
                elementAfterClarification,
                reason: ExtractMarkerReason(modelOutput, ClarificationMarker));
        }

        // Mode 1: response must be exactly one root element — no surrounding prose.
        if (!IsSingleRootElement(modelOutput))
        {
            return PrototypeElementEditResult.Rejected(
                "Model response is not a single root element (explanatory prose detected).");
        }

        // Mode 2: untargeted children must not be silently mutated.
        if (UntargetedChildrenChanged(originalOuterHtml, modelOutput))
        {
            return PrototypeElementEditResult.Rejected(
                "Model response alters untargeted child elements (child count or child text changed).");
        }

        // Mode 4 (Option A, diff-based): reject any class token added that was not on the original.
        if (UnrequestedClassAdded(originalOuterHtml, modelOutput))
        {
            return PrototypeElementEditResult.Rejected(
                "Model response introduces CSS classes absent from the original element.");
        }

        // Mode 5: id, on*, data-* attributes must all survive.
        if (RequiredAttributesDropped(originalOuterHtml, modelOutput))
        {
            return PrototypeElementEditResult.Rejected(
                "Model response drops original id, event-handler, or data-* attributes.");
        }

        return PrototypeElementEditResult.Applied(modelOutput);
    }

    // Mode 1: the entire output must be parseable as one root element.
    // A lightweight heuristic: the trimmed string must start with '<' and contain
    // no text nodes outside the root angle brackets (no leading/trailing prose).
    // ponytail: does not run a full HTML parser; a more precise check would use
    // HtmlAgilityPack — upgrade if false negatives surface in production.
    private static bool IsSingleRootElement(string output)
    {
        if (!output.StartsWith('<'))
        {
            return false;
        }

        // The root closing tag is the last '>' in the string. Any content after it
        // (stripped of whitespace) means there is trailing prose.
        var lastClose = output.LastIndexOf('>');
        if (lastClose < 0)
        {
            return false;
        }

        var afterRoot = output[(lastClose + 1)..].Trim();
        return afterRoot.Length == 0;
    }

    // Mode 2: compare direct-child count and text of non-targeted children.
    // Extracts direct children of the root element by counting immediate open tags
    // and comparing the concatenated text content.
    // ponytail: regex-based child extraction; does not handle deeply nested same-tag
    // names. Upgrade to HtmlAgilityPack if container-edit false positives appear.
    private static bool UntargetedChildrenChanged(string original, string updated)
    {
        var originalChildCount = PrototypeElementHtmlAnalysis.CountDirectChildren(original);
        var updatedChildCount = PrototypeElementHtmlAnalysis.CountDirectChildren(updated);

        if (updatedChildCount > originalChildCount)
        {
            // Model added child elements — unrequested structural change.
            return true;
        }

        // Leaf element (no child elements, only a text node): the text IS the edit
        // target, not an untargeted child. Mode 2 exists to protect sibling children
        // inside a container from silent mutation — it has no role on a leaf.
        if (originalChildCount == 0)
        {
            return false;
        }

        if (updatedChildCount < originalChildCount)
        {
            return false;
        }

        // Container: compare text inside child elements only. Root direct-text is
        // the primary edit target for this regression and must not be treated as an
        // untargeted child mutation.
        // ponytail: mode 2 still intentionally rejects edits to child-element text
        // (for example <li>Alpha</li>) even when user intent may be that child.
        // This fix does not add semantic intent parsing.
        var originalText = PrototypeElementHtmlAnalysis.ExtractChildElementsText(original);
        var updatedText = PrototypeElementHtmlAnalysis.ExtractChildElementsText(updated);

        return !string.Equals(originalText, updatedText, StringComparison.OrdinalIgnoreCase);
    }

    // Mode 4 (Option A, full-tree): extract ALL class tokens anywhere in the HTML, not
    // just the root. A child element gaining class="text-red" is still an unrequested
    // structural change — the class vocabulary is the UI kit's domain.
    // Mode 4 (Option A, diff-based): reject any class token added that was not
    // on the original. A class injected onto any child is equally invalid.
    // Tradeoff: cannot identify which element gained the class — only that an unauthorised
    // class exists somewhere in the tree. Consistent with the regex-based heuristic approach
    // used throughout this file (ponytail: upgrade to HtmlAgilityPack for per-element
    // attribution if a specific rejection message is needed).
    private static bool UnrequestedClassAdded(string original, string updated)
    {
        var originalClasses = ExtractAllClasses(original);
        var updatedClasses = ExtractAllClasses(updated);

        // Allow class replacement, but reject class addition.
        return updatedClasses.Count > originalClasses.Count;
    }

    // Mode 5: id, on*, data-* attributes must all survive unless the instruction removed them.
    private static bool RequiredAttributesDropped(string original, string updated)
    {
        var originalAttributes = ExtractTrackedAttributes(original);
        var updatedAttributes = ExtractTrackedAttributes(updated);

        return originalAttributes.Except(updatedAttributes).Any();
    }

    private static string ExtractElementAfterMarker(string modelOutput, string fallback)
    {
        var markerIndex = modelOutput.IndexOf('\n');
        if (markerIndex >= 0 && markerIndex < modelOutput.Length - 1)
        {
            return modelOutput[(markerIndex + 1)..].Trim();
        }

        return fallback;
    }

    private static string ExtractMarkerReason(string modelOutput, string marker)
    {
        var markerIndex = modelOutput.IndexOf(marker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return "(no reason provided)";
        }

        var endIndex = modelOutput.IndexOf('\n', markerIndex);
        if (endIndex < 0)
        {
            endIndex = modelOutput.Length;
        }

        var line = modelOutput[markerIndex..endIndex];
        var reasonStart = line.IndexOf(':');
        if (reasonStart < 0)
        {
            return "(no reason provided)";
        }

        return line[(reasonStart + 1)..].Trim().TrimEnd("-->".ToCharArray());
    }

    private static HashSet<string> ExtractAllClasses(string html)
    {
        var classes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(html, @"class=""([^""]*)""|class='([^']*)'"))
        {
            var classAttr = m.Groups[1].Value.Length > 0 ? m.Groups[1].Value : m.Groups[2].Value;
            foreach (var cls in classAttr.Split(SplitChars, StringSplitOptions.RemoveEmptyEntries))
            {
                classes.Add(cls);
            }
        }

        return classes;
    }

    private static HashSet<string> ExtractTrackedAttributes(string html)
    {
        var attributes = new HashSet<string>();

        // Extract id
        var idMatch = System.Text.RegularExpressions.Regex.Match(html, @"\bid=""([^""]*)""|id='([^']*)'");
        if (idMatch.Success)
        {
            attributes.Add("id");
        }

        // Extract on* handlers
        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(html, @"\bon[a-zA-Z]+="))
        {
            attributes.Add(m.Value.Trim('='));
        }

        // Extract data-* attributes
        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(html, @"\bdata-[a-zA-Z0-9\-]+="))
        {
            attributes.Add(m.Value.Trim('='));
        }

        return attributes;
    }
}
