# Plan 4 — Day 0: Prompt Interrogation & Evaluation Harness Spec

Output of the Day 0 prompt interrogation session. Two prompts drafted and stress-tested for failure modes before any implementation code. Each failure mode has a deterministic harness check. Prompt iteration on Days 1-4 is measured against these checks, not eyeballed.

---

## Locked decisions from interrogation

- **Edit architecture (A):** the model returns the complete updated element HTML; a deterministic API applies it to the postMessage-selected node. Consistent with the locked principle — LLM describes intent, API executes precisely. The Blob-iframe equivalent of `apply_to_scope`.
- **postMessage bridge (A):** send exactly the clicked element in v1. The deterministic diff-check (edit failure mode 2) is the safety net. Intent-based target narrowing deferred unless it proves a frequent real-world failure.
- **EMIS-X CSS (architectural, not prompt):** the API inlines `emis-x-base.css` (from `Infrastructure/Resources`) into the generated HTML `<head>`. The model receives `emis-x-ui-kit.md` as compositional guidance only and never reproduces the CSS.

---

## Generation prompt — draft v0.1

```
You are generating a clickable HTML demo prototype for requirements validation.
This is a throwaway reference artefact — NOT production code.

INPUTS PROVIDED TO YOU:
- UI kit selection: {emis-x | bootstrap | tailwind | custom | none}
- EMIS-X design system reference (if emis-x): {emis-x-ui-kit.md content}
- Custom CSS (if custom): {uploaded CSS content}
- Style reference image (optional): {PNG as vision input}
- Requirements: {REQ file content}

OUTPUT CONTRACT — you MUST produce exactly:
- ONE complete self-contained HTML file
- Starts with <!DOCTYPE html>, ends with </html>
- All CSS in a <style> block, all JS in a <script> block — no external files
  (except the CDN link for bootstrap/tailwind modes)
- A visible banner at the top: "PROTOTYPE ONLY — Requirements validation
  artefact. Not for production use."
- Fictional data only. Use obviously fake identifiers — NHS 000 000 0000 or
  999 999 9999 — never format-plausible numbers.

DESIGN SYSTEM RULES (switch on UI kit selection):
- emis-x: use var(--token-*) tokens only. NHS Blue is
  var(--token-colour-brand-primary). NEVER hardcode hex colours. Compose from the
  EMIS-X component vocabulary provided. The base stylesheet is already injected —
  do NOT reproduce it.
- bootstrap: include the exact Bootstrap 5 CDN <link> in <head>, use Bootstrap
  classes.
- tailwind: include the exact Tailwind CDN <script> in <head>, use Tailwind
  utilities.
- custom: use only the classes defined in the provided CSS.
- none: plain semantic HTML, minimal inline styles only if unavoidable.

SCREEN SCOPE:
- Build the primary screens the requirements describe. If more than 5 are implied,
  build the 5 most important and list the remainder in a comment.

INTERACTIVITY:
- Buttons, tabs, and links navigate between screens (show/hide via vanilla JS).
- Forms update local state. Tables populate from mock data arrays.
- The demo must be clickable but nothing persists.

If a style reference image is NOT provided, do not reason about matching any
reference — build from requirements and the UI kit alone.

DO NOT explain the code. DO NOT wrap it in markdown. Output the raw HTML only.
```

### Generation failure modes and harness checks

| # | Failure mode | Harness check | Fix |
|---|--------------|---------------|-----|
| 1 | Format-valid NHS numbers that could be mistaken for real | Regex `\d{3}\s?\d{3}\s?\d{4}` must NOT match output | Prompt mandates obviously-fake identifiers (000 000 0000) |
| 2 | Hallucinated / stale CDN URL, page renders unstyled | Fetch each CDN URL in output, assert HTTP 200 | Specify exact CDN URL in prompt for bootstrap/tailwind |
| 3 | EMIS-X demo renders unstyled (base CSS absent) | Assert `emis-x-base.css` content present in `<head>` | Resolved architecturally — API inlines base CSS; not a prompt concern |
| 4 | Phantom style-reference reasoning when no image given | Generate with no image, assert output not distorted by reference reasoning | Prompt explicitly says: no image → build from requirements + UI kit alone |
| 5 | Unbounded screen count (1 or 15) | Assert screen count is bounded and matches requirements scope | Prompt caps at 5 primary screens, notes remainder |
| 6 | Broken JS on load | Headless browser load, assert zero console errors | Interactivity constraints kept minimal in prompt |

---

## Targeted edit prompt — draft v0.1

```
You are editing ONE element in an existing HTML prototype.

You are given:
- SELECTED ELEMENT (the exact outerHTML the user clicked):
  {outerHTML}
- INSTRUCTION (what the user wants changed):
  {instruction}
- ACTIVE UI KIT: {emis-x | bootstrap | tailwind | custom | none}

YOUR OUTPUT CONTRACT — you MUST return exactly:
- The complete updated outerHTML of the SELECTED ELEMENT only
- Nothing before it, nothing after it, no markdown, no explanation
- The same root element type unless the instruction explicitly requires changing it
- Preserve all attributes, IDs, data-* attributes, and event handlers unless the
  instruction requires changing them

HARD CONSTRAINTS:
- Do NOT return the whole page. Only the selected element.
- Do NOT add sibling elements outside the selected element's root.
- Do NOT change CSS classes unless the instruction requires it. If you do, use only
  classes valid in the ACTIVE UI KIT.
- Do NOT invent new data or change text content unless the instruction requires it.
- Preserve child elements not mentioned by the instruction.

If the instruction cannot be satisfied by editing this element alone (for example it
requires changing a different element, or a parent's CSS class), return the element
UNCHANGED and prepend a single line:
  <!-- EDIT_OUT_OF_SCOPE: {one-line reason} -->

Example out-of-scope: instruction "make the header background blue" when the
background is set by a class on a parent container — return unchanged with the marker.
```

### Edit failure modes and harness checks

| # | Failure mode | Harness check | Risk |
|---|--------------|---------------|------|
| 1 | Element returned with explanatory prose | Assert response parses as exactly one element, no surrounding text; API rejects if not | Common |
| 2 | Large-container regeneration silently alters untargeted children | Diff child count and child text vs original; fail on any change beyond instruction target | **Highest** |
| 3 | Out-of-scope escape hatch under-used | For known out-of-scope instructions, assert `EDIT_OUT_OF_SCOPE` marker present | Medium |
| 4 | Invented EMIS-X classes (e.g. `text-red`) | Validate every class in returned element against `emis-x-ui-kit.md` allowlist; fail on unknown class | **Highest** |
| 5 | Dropped IDs / handlers / data-attributes breaking JS | Assert all original `id`, `on*`, `data-*` present unless instruction removed them | High |
| 6 | Ambiguous instruction → model guesses | Model returns unchanged with clarification marker; ties to AskUserQuestion pattern | Medium |

---

## The two highest residual risks

**Edit #2 — large-container regeneration.** When the user clicks a container (a whole `<table>`, a screen div), the model regenerates all children and may silently alter rows it was not asked to touch. This is the fragment-session failure reincarnated at element scale. Bridge sends the exact clicked element (decision A); the deterministic child-diff is the safety net that rejects untargeted changes before they reach the user.

**Edit #4 — invented EMIS-X classes.** "Make this red" produces `class="text-red"` which is not an EMIS-X class. The `emis-x-ui-kit.md` vocabulary is the allowlist; any returned class not in it fails the check. This is where the UI kit doc earns its place — not as guidance, as a validation allowlist.

Both have deterministic checks. That is the point of Day 0: these are caught before a user sees them, and Days 3-4 prompt iteration is measured against the checks rather than judged by eye.

---

## Evaluation harness — build note

The harness is built alongside the prompts on Days 1-4, not after. Two tiers:

- **Deterministic checks (fast, always run):** all 12 checks above are deterministic — regex, HTML parse, class allowlist lookup, child diff, headless console-error load, CDN fetch. No LLM judge needed for any of them. This is deliberate — every failure mode was designed to have a deterministic check so the harness is cheap and runs on every prompt iteration.
- **LLM judge (optional, pre-merge only):** subjective quality only — "does this look like an EMIS-X application", "was the edit instruction followed in spirit". Not required for the 12 checks; used only for the qualitative pass before merge.

A prompt version is accepted when its deterministic harness score meets threshold. Prompt A/B (frontier vs open model) is measured on the same harness — an open model carries generation or edit volume only when its score matches frontier within tolerance.

---

*Day 0 complete. Two prompts drafted, 12 failure modes identified, all with deterministic harness checks. Two architectural decisions locked (edit architecture A, bridge A). The harness spec is the foundation the Days 1-4 build is measured against.*
