using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Genesis.AI.Domain.Enums;
using Genesis.AI.Domain.Interfaces;

namespace Genesis.AI.Infrastructure.Services;

/// <summary>
/// Bedrock-backed implementation of <see cref="IPrototypeDemoEditService"/>.
/// Sends the selected element's <c>outerHTML</c> and the user's instruction to
/// <see cref="IAiService.StreamResponseAsync"/>, then runs deterministic post-
/// generation checks (failure modes 1–6) before returning a four-valued result.
///
/// Prompt cache split (mirrors Decision A from BedrockPrototypeDemoGenerationService):
///   Stable  = base edit prompt + emis-x-ui-kit.md (shared across every emis-x edit — cached ~10× cheaper)
///   Mutable = selected element outerHTML + instruction + active UI kit (per-request, always fresh)
///
/// No artefact repository or storage service is required — v1 is stateless; the
/// postMessage bridge applies the returned element client-side and nothing is
/// persisted by this service.
/// </summary>
public sealed class BedrockPrototypeDemoEditService : IPrototypeDemoEditService
{
    private const string PromptResourceName =
        "Genesis.AI.Infrastructure.Prompts.PrototypeElementEdit.md";

    private const string UiKitResourceName =
        "Genesis.AI.Infrastructure.Resources.emis-x-ui-kit.md";

    private const string OutOfScopeMarker = "EDIT_OUT_OF_SCOPE";
    private const string ClarificationMarker = "EDIT_NEEDS_CLARIFICATION";

    private static readonly HashSet<string> DepthNeutralElementNames = new(
        [
            // HTML void elements
            "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr",
            // Common SVG elements frequently emitted as leaf nodes
            "path", "circle", "ellipse", "line", "polygon", "polyline", "rect", "stop", "use", "image"
        ],
        StringComparer.OrdinalIgnoreCase);

    private readonly IAiService _aiService;

    public BedrockPrototypeDemoEditService(IAiService aiService)
    {
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
    }

    public async Task<PrototypeElementEditResult> EditElementAsync(
        Guid projectId,
        PrototypeElementEditRequest request,
        CancellationToken cancellationToken)
    {
        var prompt = LoadEmbeddedText(PromptResourceName);
        var uiKit = LoadEmbeddedText(UiKitResourceName);

        // Cache split A: stable = edit prompt + emis-x-ui-kit.md (shared/cached);
        // mutable = selected element + instruction (per-request, always fresh).
        var systemPrompt = new AiSystemPrompt(
            StablePart: BuildStablePart(prompt, uiKit),
            MutablePart: BuildMutablePart(request));

        var userMessage = new AiMessage(
            MessageRole.User,
            $"Edit the selected element as instructed. Return only the updated outerHTML.");

        var buffer = new StringBuilder();
        await foreach (var chunk in _aiService.StreamResponseAsync(systemPrompt, [userMessage], cancellationToken))
        {
            buffer.Append(chunk);
        }

        var result = Validate(buffer.ToString().Trim(), request.SelectedOuterHtml);
        if (result.Status != PrototypeElementEditStatus.Applied)
        {
            return result;
        }

        // Applied: perform the element replacement server-side against the full document so
        // the client renders the returned document directly. Locate the element by a
        // serialisation-independent fingerprint (self-closing SVG tags etc. defeat a raw match).
        var updatedFullHtml = await PrototypeElementReplacer.ReplaceElementAsync(
            request.CurrentHtml, request.SelectedOuterHtml, result.UpdatedOuterHtml, cancellationToken);
        if (updatedFullHtml is null)
        {
            return PrototypeElementEditResult.Rejected(
                "Selected element could not be located in the current prototype document.");
        }

        return result with { UpdatedFullHtml = updatedFullHtml };
    }

    // --- Deterministic post-generation validation (failure modes 1–6) ---

    private static PrototypeElementEditResult Validate(string modelOutput, string originalOuterHtml)
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
        var originalChildCount = CountDirectChildren(original);
        var updatedChildCount = CountDirectChildren(updated);

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
        var originalText = ExtractChildElementsText(original);
        var updatedText = ExtractChildElementsText(updated);

        return !string.Equals(originalText, updatedText, StringComparison.OrdinalIgnoreCase);
    }

    // Mode 4 (Option A, full-tree): extract ALL class tokens anywhere in the HTML, not
    // just the root element. A class injected onto any child is equally invalid.
    // Tradeoff: cannot identify which element gained the class — only that an unauthorised
    // class exists somewhere in the tree. Consistent with the regex-based heuristic approach
    // used throughout this file (ponytail: upgrade to HtmlAgilityPack for per-element
    // attribution if a specific rejection message is needed).
    private static bool UnrequestedClassAdded(string original, string updated)
    {
        var originalClasses = ExtractAllClasses(original);
        var updatedClasses = ExtractAllClasses(updated);

        // Allow class replacement (e.g. btn-primary → btn-danger) but reject
        // class addition (more classes than original indicates unrequested structure).
        return updatedClasses.Count > originalClasses.Count;
    }

    // Mode 5: every id, on*, data-* attribute on the original root must appear on the updated root.
    private static bool RequiredAttributesDropped(string original, string updated)
    {
        var originalAttrs = ExtractTrackedAttributes(original);
        var updatedAttrs = ExtractTrackedAttributes(updated);

        foreach (var attr in originalAttrs)
        {
            if (!updatedAttrs.Contains(attr))
            {
                return true;
            }
        }

        return false;
    }

    // --- Extraction helpers ---

    private static string ExtractElementAfterMarker(string modelOutput, string fallback)
    {
        var commentEnd = modelOutput.IndexOf("-->", StringComparison.Ordinal);
        if (commentEnd < 0)
        {
            return fallback;
        }

        return modelOutput[(commentEnd + 3)..].Trim();
    }

    private static string ExtractMarkerReason(string modelOutput, string marker)
    {
        var start = modelOutput.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
        {
            return string.Empty;
        }

        var colonPos = modelOutput.IndexOf(':', start);
        var endPos = modelOutput.IndexOf("-->", start, StringComparison.Ordinal);
        if (colonPos < 0 || endPos < 0 || colonPos >= endPos)
        {
            return string.Empty;
        }

        return modelOutput[(colonPos + 1)..endPos].Trim();
    }

    private static int CountDirectChildren(string html)
    {
        // Count immediate opening tags inside the root element's content.
        // Strip the outer root tag first, then count top-level '<' that are not '/'.
        var inner = ExtractInnerHtml(html);
        return Regex.Count(inner, @"<(?!/)[a-zA-Z]");
    }

    private static string ExtractInnerHtml(string html)
    {
        var firstClose = html.IndexOf('>');
        if (firstClose < 0 || firstClose >= html.Length - 1)
        {
            return string.Empty;
        }

        var lastOpen = html.LastIndexOf('<');
        if (lastOpen <= firstClose)
        {
            return string.Empty;
        }

        return html[(firstClose + 1)..lastOpen];
    }

    private static string ExtractChildElementsText(string html)
    {
        var inner = ExtractInnerHtml(html);
        if (string.IsNullOrWhiteSpace(inner))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(inner.Length);
        var depth = 0;

        for (var index = 0; index < inner.Length; index++)
        {
            if (inner[index] != '<')
            {
                if (depth >= 1)
                {
                    builder.Append(inner[index]);
                }

                continue;
            }

            var closeIndex = inner.IndexOf('>', index + 1);
            if (closeIndex < 0)
            {
                break;
            }

            var rawTag = inner[(index + 1)..closeIndex].Trim();
            var isClosing = rawTag.Length > 0 && rawTag[0] == '/';
            var isSelfClosing = rawTag.Length > 0 && rawTag[^1] == '/';
            var tagName = ExtractTagName(rawTag);
            var isDepthNeutral = isSelfClosing || DepthNeutralElementNames.Contains(tagName);

            if (isClosing)
            {
                depth = Math.Max(0, depth - 1);
            }
            else if (!isDepthNeutral)
            {
                depth++;
            }

            // Skip to the end of the current tag.
            index = closeIndex;
        }

        // ponytail: comments and CDATA are treated as generic tags here; if they
        // become common in model output, upgrade to a parser-backed implementation.
        return builder.ToString().Trim();
    }

    private static string ExtractTagName(string rawTag)
    {
        if (string.IsNullOrWhiteSpace(rawTag))
        {
            return string.Empty;
        }

        var start = rawTag[0] == '/' ? 1 : 0;
        if (start >= rawTag.Length)
        {
            return string.Empty;
        }

        var end = start;
        while (end < rawTag.Length)
        {
            var value = rawTag[end];
            if (char.IsWhiteSpace(value) || value == '/' || value == '>')
            {
                break;
            }

            end++;
        }

        return rawTag[start..end];
    }

    // Scans the entire HTML string for all class="..." occurrences and returns the
    // union of all class tokens. Used by UnrequestedClassAdded for full-tree coverage
    // (a class injected onto any child is equally invalid, not just the root element).
    // ponytail: cannot identify which element gained the class — only that an unauthorised
    // class exists somewhere in the tree. Upgrade to HtmlAgilityPack for per-element
    // attribution if a specific rejection message is needed.
    private static HashSet<string> ExtractAllClasses(string html)
    {
        var classes = new HashSet<string>(StringComparer.Ordinal);
        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(
            html, @"class=""([^""]*)""|class='([^']*)'" ))
        {
            var raw = m.Groups[1].Value.Length > 0 ? m.Groups[1].Value : m.Groups[2].Value;
            foreach (var cls in raw.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                classes.Add(cls);
            }
        }

        return classes;
    }

    private static HashSet<string> ExtractTrackedAttributes(string html)
    {
        var rootTag = ExtractRootTag(html);
        var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // id
        var idMatch = System.Text.RegularExpressions.Regex.Match(rootTag, @"\bid=""([^""]*)""\B|\bid='([^']*)'");
        if (idMatch.Success)
        {
            var id = idMatch.Groups[1].Value.Length > 0 ? idMatch.Groups[1].Value : idMatch.Groups[2].Value;
            results.Add($"id={id}");
        }

        // on* handlers — capture name only (value may legitimately change with code refactors)
        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(rootTag, @"\bon[a-zA-Z]+="))
        {
            results.Add(m.Value.TrimEnd('=').ToLowerInvariant());
        }

        // data-* attributes — capture name only
        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(rootTag, @"\bdata-[a-zA-Z0-9\-]+="))
        {
            results.Add(m.Value.TrimEnd('=').ToLowerInvariant());
        }

        return results;
    }

    private static string ExtractRootTag(string html)
    {
        var end = html.IndexOf('>');
        return end < 0 ? html : html[..(end + 1)];
    }

    // --- System prompt builders ---

    private static string BuildStablePart(string prompt, string uiKit)
    {
        return $"""
            {prompt}

            ## EMIS-X Design System Reference

            {uiKit}
            """;
    }

    private static string BuildMutablePart(PrototypeElementEditRequest request)
    {
        return $"""
            ## Edit Request

            SELECTED ELEMENT:
            {request.SelectedOuterHtml}

            INSTRUCTION:
            {request.Instruction}

            ACTIVE UI KIT: {request.ActiveUiKit}
            """;
    }

    private static string LoadEmbeddedText(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
