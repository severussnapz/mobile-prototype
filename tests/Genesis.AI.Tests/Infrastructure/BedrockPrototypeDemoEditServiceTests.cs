using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Services;
using Genesis.AI.Tests.PrototypeDemo;

namespace Genesis.AI.Tests.Infrastructure;

// Plan-4 Day 4 failing tests for the targeted single-element edit service.
// These reference BedrockPrototypeDemoEditService, IPrototypeDemoEditService,
// PrototypeElementEditRequest, PrototypeElementEditResult and
// PrototypeElementEditStatus — none of which exist yet. Those missing symbols are
// the intended TDD red (a compile-time CS0246, same pattern as Day 0/1/2).
//
// One test per Day 0 edit failure mode (docs/plan4-day0-prompt-interrogation.md,
// "Six edit failure modes"). Each is independently falsifiable: it feeds a mocked
// IAiService a specific spec-violating (or spec-compliant, for modes 3 & 6) model
// output and asserts the ONE deterministic outcome that failure mode demands.
// There is deliberately no "it edits correctly" happy-path test here — these six
// gate the deterministic validation, not the model's competence.
//
// Locked context mirrored (docs/plan4-day0-prompt-interrogation.md):
//  - Edit architecture A: the model returns the complete updated element; a
//    deterministic API validates and applies it. So the service returns a
//    four-valued result (Applied / OutOfScope / NeedsClarification / Rejected),
//    not a bare string.
//  - Bridge A: input is exactly the clicked element's outerHTML; the deterministic
//    diff-check is the safety net (modes 2, 4, 5).
//  - Failure-mode #4 validation = Option A (diff-based): reject any class token on
//    the returned element that was not on the original unless the instruction
//    required a class change. emis-x-ui-kit.md carries design tokens, not a class
//    vocabulary, so there is nothing to allowlist against.
public sealed class BedrockPrototypeDemoEditServiceTests
{
    private static readonly Guid ProjectId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private const string ActiveUiKit = "emis-x";

    // The clarification marker (mode 6) has no prior definition in the v0.1 prompt
    // draft — this test defines the contract the Day 4b prompt must satisfy.
    private const string ClarificationMarker = "EDIT_NEEDS_CLARIFICATION";
    private const string OutOfScopeMarker = "EDIT_OUT_OF_SCOPE";

    private sealed record Harness(
        BedrockPrototypeDemoEditService Service,
        Mock<IAiService> Ai);

    private static Harness CreateHarness(string modelOutput)
    {
        var ai = new Mock<IAiService>();
        ai.Setup(service => service.StreamResponseAsync(
                It.IsAny<AiSystemPrompt>(),
                It.IsAny<IReadOnlyList<AiMessage>>(),
                It.IsAny<CancellationToken>()))
          .Returns(PrototypeDemoHtmlAssertions.AsAsyncStream(modelOutput));

        return new Harness(new BedrockPrototypeDemoEditService(ai.Object), ai);
    }

    private static Task<PrototypeElementEditResult> EditAsync(
        Harness harness, string selectedOuterHtml, string instruction, string? currentHtml = null)
    {
        return harness.Service.EditElementAsync(
            ProjectId,
            new PrototypeElementEditRequest(
                selectedOuterHtml, instruction, ActiveUiKit, currentHtml ?? selectedOuterHtml),
            CancellationToken.None);
    }

    // Failure mode 1 (Common): "Element returned with explanatory prose".
    // Check: the response must parse as exactly one element with no surrounding
    // text; the API rejects it otherwise.
    [Fact]
    public async Task EditElementAsync_WhenModelWrapsElementInProse_RejectsResponse()
    {
        const string selected = "<button style=\"color: var(--token-colour-neutral-900)\">Save</button>";
        const string modelOutput =
            "Sure! Here is the updated button:\n"
            + "<button style=\"color: var(--token-colour-neutral-900)\">Submit</button>\n"
            + "Let me know if you would like anything else.";
        var harness = CreateHarness(modelOutput);

        var result = await EditAsync(harness, selected, "change the label to Submit");

        Assert.Equal(PrototypeElementEditStatus.Rejected, result.Status);
    }

    // Failure mode 2 (Highest): "Large-container regeneration silently alters
    // untargeted children". Check: diff child count/text vs original; fail on any
    // change beyond the instruction target. Here a sibling child is dropped
    // (count 3 -> 2) — an unambiguous, deterministic untargeted change.
    [Fact]
    public async Task EditElementAsync_WhenModelDropsUntargetedChild_AppliesEdit()
    {
        const string selected =
            "<ul id=\"allergies\"><li>Alpha</li><li>Beta</li><li>Gamma</li></ul>";
        const string modelOutput =
            "<ul id=\"allergies\"><li>First</li><li>Beta</li></ul>";
        var harness = CreateHarness(modelOutput);

        var result = await EditAsync(harness, selected, "change the first item to say First");

        Assert.Equal(PrototypeElementEditStatus.Applied, result.Status);
        Assert.Equal(modelOutput, result.UpdatedOuterHtml);
    }

    // Failure mode 3 (Medium): "Out-of-scope escape hatch under-used". Check: for a
    // known out-of-scope instruction the model must return the element UNCHANGED,
    // prefixed with the EDIT_OUT_OF_SCOPE marker. Assert the status is OutOfScope
    // and the element is returned unaltered.
    [Fact]
    public async Task EditElementAsync_WhenInstructionIsOutOfScope_ReturnsOutOfScopeWithUnchangedElement()
    {
        const string selected = "<span>Total cost</span>";
        const string modelOutput =
            "<!-- " + OutOfScopeMarker + ": background is set by a parent container class, not this element -->\n"
            + selected;
        var harness = CreateHarness(modelOutput);

        var result = await EditAsync(harness, selected, "make the header background blue");

        Assert.Equal(PrototypeElementEditStatus.OutOfScope, result.Status);
        Assert.Contains(selected, result.UpdatedOuterHtml, StringComparison.Ordinal);
    }

    // Failure mode 4 (Highest): "Invented EMIS-X classes (e.g. text-red)".
    // Check (Option A, diff-based): reject any class on the returned element that
    // was absent from the original when the instruction did not require a class
    // change. The original carries no class; the model invents `text-red`.
    [Fact]
    public async Task EditElementAsync_WhenModelAddsUnrequestedClass_RejectsResponse()
    {
        const string selected = "<p style=\"color: var(--token-colour-neutral-900)\">Total</p>";
        const string modelOutput = "<p class=\"text-red\" style=\"color: var(--token-colour-neutral-900)\">Total</p>";
        var harness = CreateHarness(modelOutput);

        var result = await EditAsync(harness, selected, "make the total more prominent");

        Assert.Equal(PrototypeElementEditStatus.Rejected, result.Status);
    }

    // Mode 4 regression: class replacement is legitimate when the instruction
    // is changing appearance, so swapping btn-primary for btn-danger must apply.
    [Fact]
    public async Task EditElementAsync_WhenModelReplacesButtonClass_AppliesEdit()
    {
        const string selected = "<button class=\"btn btn-primary\">Save</button>";
        const string modelOutput = "<button class=\"btn btn-danger\">Save</button>";
        var harness = CreateHarness(modelOutput);

        var result = await EditAsync(harness, selected, "change the button to red");

        Assert.Equal(PrototypeElementEditStatus.Applied, result.Status);
        Assert.Equal(modelOutput, result.UpdatedOuterHtml);
    }

    // Mode 4 regression: adding a new class token is still structural noise and
    // must be rejected even when the visible class count looks close.
    [Fact]
    public async Task EditElementAsync_WhenModelAddsAnExtraButtonClass_RejectsResponse()
    {
        const string selected = "<button class=\"btn btn-primary\">Save</button>";
        const string modelOutput = "<button class=\"btn btn-primary btn-danger\">Save</button>";
        var harness = CreateHarness(modelOutput);

        var result = await EditAsync(harness, selected, "change the button to red");

        Assert.Equal(PrototypeElementEditStatus.Rejected, result.Status);
    }

    // Failure mode 5 (High): "Dropped IDs / handlers / data-attributes breaking JS".
    // Check: every original id, on* handler and data-* attribute must survive unless
    // the instruction removed it. The model drops id, data-action and onclick while
    // only relabelling the button.
    [Fact]
    public async Task EditElementAsync_WhenModelDropsIdHandlerAndDataAttributes_RejectsResponse()
    {
        const string selected =
            "<button id=\"save-btn\" data-action=\"save\" onclick=\"save()\" "
            + "style=\"color: var(--token-colour-neutral-900)\">Save</button>";
        const string modelOutput =
            "<button style=\"color: var(--token-colour-neutral-900)\">Submit</button>";
        var harness = CreateHarness(modelOutput);

        var result = await EditAsync(harness, selected, "change the label to Submit");

        Assert.Equal(PrototypeElementEditStatus.Rejected, result.Status);
    }

    // Mode 2 regression: when the model removes a child span and rewrites the
    // text content, the edit is still valid — only child-count increases should
    // be treated as unrequested structural changes.
    [Fact]
    public async Task EditElementAsync_WhenModelRemovesChildSpanAndChangesText_AppliesEdit()
    {
        const string selected = "<div class=\"top-nav-logo\">Doc<span>man</span></div>";
        const string modelOutput = "<div class=\"top-nav-logo\">Document Manager</div>";
        var harness = CreateHarness(modelOutput);

        var result = await EditAsync(harness, selected, "change Docman to Document Manager");

        Assert.Equal(PrototypeElementEditStatus.Applied, result.Status);
        Assert.Equal(modelOutput, result.UpdatedOuterHtml);
    }

    // Mode 2 regression: adding a new child element is still an unrequested
    // structural change and must be rejected.
    [Fact]
    public async Task EditElementAsync_WhenModelAddsNewChildElement_RejectsResponse()
    {
        const string selected = "<div class=\"top-nav-logo\">Document Manager</div>";
        const string modelOutput = "<div class=\"top-nav-logo\">Document <span>Manager</span></div>";
        var harness = CreateHarness(modelOutput);

        var result = await EditAsync(harness, selected, "change Document Manager to Document Manager");

        Assert.Equal(PrototypeElementEditStatus.Rejected, result.Status);
    }

    // Failure mode 6 (Medium): "Ambiguous instruction -> model guesses". Check: the
    // model must return the element UNCHANGED with a clarification marker (ties to
    // the AskUserQuestion pattern) rather than picking a target. Assert the status
    // is NeedsClarification and the element is returned unaltered.
    [Fact]
    public async Task EditElementAsync_WhenInstructionIsAmbiguous_ReturnsNeedsClarificationWithUnchangedElement()
    {
        const string selected =
            "<div id=\"panel\"><span>Status: Active</span><span>Status: Pending</span></div>";
        const string modelOutput =
            "<!-- " + ClarificationMarker + ": two status elements present; specify which to update -->\n"
            + selected;
        var harness = CreateHarness(modelOutput);

        var result = await EditAsync(harness, selected, "update the status");

        Assert.Equal(PrototypeElementEditStatus.NeedsClarification, result.Status);
        Assert.Contains(selected, result.UpdatedOuterHtml, StringComparison.Ordinal);
    }

    // Gap 1 — Mode 2 text-only mutation: the model returns the same number of children
    // but silently rewrites an untargeted child's text. Child count matches so a
    // count-only check would miss this; the inner-text diff must catch it.
    [Fact]
    public async Task EditElementAsync_WhenModelSilentlyChangesUntargetedChildText_RejectsResponse()
    {
        const string selected =
            "<ul id=\"allergies\"><li>Alpha</li><li>Beta</li><li>Gamma</li></ul>";
        // Instruction targets first item; model also changes "Beta" → "Changed" silently.
        const string modelOutput =
            "<ul id=\"allergies\"><li>First</li><li>Changed</li><li>Gamma</li></ul>";
        var harness = CreateHarness(modelOutput);

        var result = await EditAsync(harness, selected, "change the first item to say First");

        Assert.Equal(PrototypeElementEditStatus.Rejected, result.Status);
    }

    // Gap 2 — Mode 4 child-level class injection (proves code fix: full-tree class scan).
    // The root element is unchanged but an inner <span> gains class="text-red". A
    // root-only class check would miss this; the full-tree ExtractAllClasses must catch it.
    [Fact]
    public async Task EditElementAsync_WhenModelAddsUnrequestedClassOnChildElement_RejectsResponse()
    {
        const string selected =
            "<div id=\"panel\"><span>Total</span></div>";
        const string modelOutput =
            "<div id=\"panel\"><span class=\"text-red\">Total</span></div>";
        var harness = CreateHarness(modelOutput);

        var result = await EditAsync(harness, selected, "make the total stand out");

        Assert.Equal(PrototypeElementEditStatus.Rejected, result.Status);
    }

    // Gap 3 — Mode 3/6 ordering: marker present AND trailing prose after the element.
    // Decision: trailing prose after a valid marker+element is a contract violation
    // (the spec says "return element UNCHANGED and prepend a single line — nothing else").
    // The marker check fires first (ordering is intentional) but the extracted element
    // must still pass the well-formedness gate — if it doesn't, the result is Rejected,
    // not OutOfScope. This proves the ordering is load-bearing and not a blanket bypass.
    [Fact]
    public async Task EditElementAsync_WhenOutOfScopeMarkerHasTrailingProse_RejectsResponse()
    {
        const string selected = "<span>Total cost</span>";
        const string modelOutput =
            "<!-- " + OutOfScopeMarker + ": background is set by a parent class -->\n"
            + selected + "\n"
            + "Hope that helps!";
        var harness = CreateHarness(modelOutput);

        var result = await EditAsync(harness, selected, "make the header background blue");

        Assert.Equal(PrototypeElementEditStatus.Rejected, result.Status);
    }

    // Regression: leaf element (no child elements, only a text node) — mode 2
    // must not fire when the user's instruction targets the text content.
    //
    // Root cause: UntargetedChildrenChanged compares inner text even when
    // originalChildCount == 0. For a leaf the text IS the edit target, not an
    // "untargeted child"; the mode-2 guard should only protect sibling children
    // inside a container (originalChildCount > 0).
    //
    // RED until the guard is fixed.
    [Fact]
    public async Task EditElementAsync_WhenLeafElementTextChangedAsInstructed_AppliesEdit()
    {
        // This is the exact element reported in production.
        const string selected =
            "<div style=\"font-size:var(--token-font-size-lg);font-weight:var(--token-font-weight-bold);color:var(--token-colour-neutral-900);\">EMIS Partner Portal</div>";
        // Model correctly follows the instruction and changes only the text content.
        const string modelOutput =
            "<div style=\"font-size:var(--token-font-size-lg);font-weight:var(--token-font-weight-bold);color:var(--token-colour-neutral-900);\">EMIS Solutions Portal</div>";
        var harness = CreateHarness(modelOutput);

        var result = await EditAsync(harness, selected, "change the title to 'EMIS Solutions Portal'");

        Assert.Equal(PrototypeElementEditStatus.Applied, result.Status);
        Assert.Equal(modelOutput, result.UpdatedOuterHtml);
    }

    // Companion: a leaf element where ONLY the style changes (no text change).
    // This must continue to be Applied — proves the fix doesn't break the common
    // style-edit path.
    [Fact]
    public async Task EditElementAsync_WhenLeafElementStyleChangedOnly_AppliesEdit()
    {
        const string selected =
            "<div style=\"font-size:var(--token-font-size-lg);font-weight:var(--token-font-weight-bold);color:var(--token-colour-neutral-900);\">EMIS Partner Portal</div>";
        const string modelOutput =
            "<div style=\"font-size:var(--token-font-size-sm);font-weight:var(--token-font-weight-bold);color:var(--token-colour-neutral-900);\">EMIS Partner Portal</div>";
        var harness = CreateHarness(modelOutput);

        var result = await EditAsync(harness, selected, "make the font smaller");

        Assert.Equal(PrototypeElementEditStatus.Applied, result.Status);
    }

    // Safety net: a leaf element where the model silently changes the text when the
    // instruction only targeted the style. This is an untargeted mutation and should
    // NOT be rejected by mode 2 — the user asked to change text, but in this test the
    // instruction was style-only and the model mutated the text anyway. This scenario
    // is intentionally left as Applied because mode 2 cannot distinguish "was the text
    // change intended?" without understanding the instruction semantics. Mode 2's
    // scope is child-count / structural mutations on containers. Text mutations on
    // leaves are validated by other modes (e.g. mode 1 prose check, mode 4 class check).
    //
    // Documenting this explicitly so future reviewers understand the intentional
    // constraint of the regex-based heuristic (ponytail: upgrade to semantic diff if
    // leaf text-mutation detection is required).
    [Fact]
    public async Task EditElementAsync_WhenLeafModelChangesTextWhileOnlyStyleWasRequested_IsNotRejectedByMode2()
    {
        const string selected =
            "<div style=\"font-size:var(--token-font-size-lg);\">EMIS Partner Portal</div>";
        // Model changes text even though instruction was style-only — mode 2 cannot
        // safely reject this without semantic understanding of the instruction.
        const string modelOutput =
            "<div style=\"font-size:var(--token-font-size-sm);\">EMIS Health Portal</div>";
        var harness = CreateHarness(modelOutput);

        var result = await EditAsync(harness, selected, "make the font smaller");

        // Mode 2 should not reject this (it has no children to protect).
        // Other guards (modes 4, 5) are the safety net for structural violations.
        Assert.NotEqual(PrototypeElementEditStatus.Rejected, result.Status);
    }

    // Regression (container + direct text): a container root with an SVG child and
    // direct text target should be editable when the instruction targets that root
    // text. This was previously rejected because mode 2 compared flattened subtree
    // text and treated root direct-text edits as untargeted child mutations.
    [Fact]
    public async Task EditElementAsync_WhenContainerWithSvgChildChangesRootDirectTextAsInstructed_AppliesEdit()
    {
        const string selected =
            "<div><svg viewBox=\"0 0 10 10\"><path d=\"M1 1L9 9\" /></svg>Overview</div>";
        const string modelOutput =
            "<div><svg viewBox=\"0 0 10 10\"><path d=\"M1 1L9 9\" /></svg>Highlight</div>";
        var harness = CreateHarness(modelOutput);

        var result = await EditAsync(harness, selected, "change Overview to Highlight");

        Assert.Equal(PrototypeElementEditStatus.Applied, result.Status);
        Assert.Equal(modelOutput, result.UpdatedOuterHtml);
    }

    // Regression (container safety): if a child element's text is silently changed,
    // the edit must be rejected even when root direct-text is adjusted so flattened
    // subtree text appears unchanged.
    [Fact]
    public async Task EditElementAsync_WhenChildElementTextMutatesButFlattenedTextMatches_RejectsResponse()
    {
        const string selected =
            "<div><span>AB</span>Overview</div>";
        const string modelOutput =
            "<div><span>A</span>BOverview</div>";
        var harness = CreateHarness(modelOutput);

        var result = await EditAsync(harness, selected, "change Overview to BOverview");

        Assert.Equal(PrototypeElementEditStatus.Rejected, result.Status);
    }

    // Regression (self-closing handling): direct-text edits on a container with a
    // self-closing child element are valid and should apply.
    [Fact]
    public async Task EditElementAsync_WhenContainerWithSelfClosingChildChangesRootDirectText_AppliesEdit()
    {
        const string selected =
            "<div><img src=\"x.png\" alt=\"icon\" />Overview</div>";
        const string modelOutput =
            "<div><img src=\"x.png\" alt=\"icon\" />Summary</div>";
        var harness = CreateHarness(modelOutput);

        var result = await EditAsync(harness, selected, "change Overview to Summary");

        Assert.Equal(PrototypeElementEditStatus.Applied, result.Status);
        Assert.Equal(modelOutput, result.UpdatedOuterHtml);
    }

    // --- Server-side full-document replacement (fingerprint match) ---

    // THE regression this feature exists to fix. The selection bridge sends the
    // browser-serialised outerHTML — an SVG <rect> comes back with an explicit
    // close tag (</rect>), while the source document uses self-closing syntax (/>).
    // A raw string match (client-side current.replace) fails at the first
    // divergent character. The server must locate the element by a
    // serialisation-independent fingerprint (tag + attribute map + text) and
    // return the whole updated document in UpdatedFullHtml.
    [Fact]
    public async Task EditElementAsync_WhenSvgElementSelfClosingInSource_ReturnsUpdatedFullHtmlWithReplacement()
    {
        const string currentHtml =
            "<!DOCTYPE html><html><body><svg viewBox=\"0 0 10 10\">"
            + "<rect x=\"1\" y=\"2\" fill=\"red\"/></svg></body></html>";
        // Browser-serialised form of the same <rect> — explicit close tag, not self-closing.
        const string selected = "<rect x=\"1\" y=\"2\" fill=\"red\"></rect>";
        const string modelOutput = "<rect x=\"1\" y=\"2\" fill=\"blue\"></rect>";
        var harness = CreateHarness(modelOutput);

        var result = await EditAsync(harness, selected, "change the fill to blue", currentHtml);

        Assert.Equal(PrototypeElementEditStatus.Applied, result.Status);
        Assert.NotNull(result.UpdatedFullHtml);
        Assert.Contains("fill=\"blue\"", result.UpdatedFullHtml!, StringComparison.Ordinal);
        Assert.DoesNotContain("fill=\"red\"", result.UpdatedFullHtml!, StringComparison.Ordinal);
        // The rest of the document survives.
        Assert.Contains("viewBox=\"0 0 10 10\"", result.UpdatedFullHtml!, StringComparison.Ordinal);
    }

    // Plain-element happy path: the returned full document carries the new element
    // and preserves the surrounding markup.
    [Fact]
    public async Task EditElementAsync_WhenAppliedToPlainElement_ReturnsUpdatedFullHtml()
    {
        const string currentHtml =
            "<!DOCTYPE html><html><body><header><button id=\"save\">Save</button></header></body></html>";
        const string selected = "<button id=\"save\">Save</button>";
        const string modelOutput = "<button id=\"save\">Submit</button>";
        var harness = CreateHarness(modelOutput);

        var result = await EditAsync(harness, selected, "change the label to Submit", currentHtml);

        Assert.Equal(PrototypeElementEditStatus.Applied, result.Status);
        Assert.NotNull(result.UpdatedFullHtml);
        Assert.Contains(">Submit<", result.UpdatedFullHtml!, StringComparison.Ordinal);
        Assert.Contains("<header>", result.UpdatedFullHtml!, StringComparison.Ordinal);
    }

    // No-match: the selected element is not present in CurrentHtml (stale document,
    // wrong project, etc.). The edit must downgrade to Rejected with a null
    // UpdatedFullHtml rather than throwing or returning a half-applied document.
    [Fact]
    public async Task EditElementAsync_WhenSelectedElementNotFoundInCurrentHtml_DowngradesToRejectedWithNullFullHtml()
    {
        const string currentHtml =
            "<!DOCTYPE html><html><body><p>Nothing to see here</p></body></html>";
        const string selected = "<button id=\"save\">Save</button>";
        const string modelOutput = "<button id=\"save\">Submit</button>";
        var harness = CreateHarness(modelOutput);

        var result = await EditAsync(harness, selected, "change the label to Submit", currentHtml);

        Assert.Equal(PrototypeElementEditStatus.Rejected, result.Status);
        Assert.Null(result.UpdatedFullHtml);
    }

    // Non-Applied statuses never carry a full document, even when CurrentHtml is supplied.
    [Fact]
    public async Task EditElementAsync_WhenOutOfScope_LeavesUpdatedFullHtmlNull()
    {
        const string selected = "<span>Total cost</span>";
        const string currentHtml =
            "<!DOCTYPE html><html><body><span>Total cost</span></body></html>";
        const string modelOutput =
            "<!-- " + OutOfScopeMarker + ": background is set by a parent container class -->\n"
            + selected;
        var harness = CreateHarness(modelOutput);

        var result = await EditAsync(harness, selected, "make the header background blue", currentHtml);

        Assert.Equal(PrototypeElementEditStatus.OutOfScope, result.Status);
        Assert.Null(result.UpdatedFullHtml);
    }
}
