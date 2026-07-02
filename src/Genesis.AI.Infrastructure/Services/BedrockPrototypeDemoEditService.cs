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

        return Validate(buffer.ToString().Trim(), request.SelectedOuterHtml);
    }

    // --- Deterministic post-generation validation (failure modes 1–6) ---

    private static PrototypeElementEditResult Validate(string modelOutput, string originalOuterHtml)
    {
        // Mode 3: EDIT_OUT_OF_SCOPE — check before prose check because the marker
        // is a well-formed comment and the element is intentionally unchanged.
        if (modelOutput.Contains(OutOfScopeMarker, StringComparison.Ordinal))
        {
            return PrototypeElementEditResult.OutOfScope(
                ExtractElementAfterMarker(modelOutput, originalOuterHtml),
                reason: ExtractMarkerReason(modelOutput, OutOfScopeMarker));
        }

        // Mode 6: EDIT_NEEDS_CLARIFICATION — same pattern.
        if (modelOutput.Contains(ClarificationMarker, StringComparison.Ordinal))
        {
            return PrototypeElementEditResult.NeedsClarification(
                ExtractElementAfterMarker(modelOutput, originalOuterHtml),
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

        if (originalChildCount != updatedChildCount)
        {
            return true;
        }

        // If child count is the same, compare normalised inner text to catch silent
        // text substitutions on unmentioned children.
        var originalText = ExtractInnerText(original);
        var updatedText = ExtractInnerText(updated);

        return !string.Equals(originalText, updatedText, StringComparison.OrdinalIgnoreCase);
    }

    // Mode 4 (Option A): extract all class tokens from the root element's class attribute.
    // Reject if the updated element carries any class absent from the original.
    private static bool UnrequestedClassAdded(string original, string updated)
    {
        var originalClasses = ExtractRootClasses(original);
        var updatedClasses = ExtractRootClasses(updated);

        foreach (var cls in updatedClasses)
        {
            if (!originalClasses.Contains(cls))
            {
                return true;
            }
        }

        return false;
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

    private static string ExtractInnerText(string html)
    {
        return Regex.Replace(html, @"<[^>]+>", string.Empty).Trim();
    }

    private static HashSet<string> ExtractRootClasses(string html)
    {
        var rootTag = ExtractRootTag(html);
        var match = System.Text.RegularExpressions.Regex.Match(
            rootTag, @"class=""([^""]*)""|class='([^']*)'");

        var raw = match.Success
            ? (match.Groups[1].Value.Length > 0 ? match.Groups[1].Value : match.Groups[2].Value)
            : string.Empty;

        return raw.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
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
