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
        Harness harness, string selectedOuterHtml, string instruction)
    {
        return harness.Service.EditElementAsync(
            ProjectId,
            new PrototypeElementEditRequest(selectedOuterHtml, instruction, ActiveUiKit),
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
    public async Task EditElementAsync_WhenModelDropsUntargetedChild_RejectsResponse()
    {
        const string selected =
            "<ul id=\"allergies\"><li>Alpha</li><li>Beta</li><li>Gamma</li></ul>";
        const string modelOutput =
            "<ul id=\"allergies\"><li>First</li><li>Beta</li></ul>";
        var harness = CreateHarness(modelOutput);

        var result = await EditAsync(harness, selected, "change the first item to say First");

        Assert.Equal(PrototypeElementEditStatus.Rejected, result.Status);
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
}
