# Genesis AI — Module 2: Prototype Builder
## Role: BA / PO / Designer | Est. 30 minutes
### Prerequisite: Module 0 complete. Module 1 recommended first.

---

## What This Module Covers

P02 turns an approved REQ file into a clickable, styled EMIS-X prototype in minutes. The prototype is not a wireframe — it is a real HTML file rendered in a sandboxed browser, built on the EMIS-X design system. You can click it, scroll it, and show it to a clinical user in a meeting.

The prototype validates UX decisions before any engineering cost is committed. Issues found here take minutes to fix. Issues found at code review take days.

---

## What P02 Produces

A single HTML file: `prototype/index.html`

This file is:
- A self-contained, clickable prototype anchored on the EMIS-X UI kit
- Rendered live in a sandboxed iframe in the tool — no deployment required
- Versioned in S3 — every previous version is recoverable
- Approved by a human before P03 begins

It is explicitly marked "PROTOTYPE ONLY" — it is not production code, not a design system source of truth, and not a technical specification.

---

## Three Ways to Work in P02

### 1. Generation — describe the feature, get a prototype

Type a plain-English description of the feature. The agent asks clarifying questions, then generates a complete, styled prototype. This is the starting point for every new prototype.

**What to include in your description:**
- The primary user action ("a GP viewing their appointment list for the day")
- The key data elements visible on screen ("appointment time, patient name, appointment type, status")
- The primary interaction ("clicking a row opens the appointment detail")
- Any known EMIS-X UI patterns to follow ("use the same list pattern as the existing task manager")

**What not to include:**
- CSS instructions ("make it blue") — the EMIS-X UI kit handles colour
- Layout specifications ("put the search bar on the left") — describe the intent, let the UI kit handle the layout
- Technical implementation details — this is a prototype, not a spec

### 2. Surgical Edit — right-click any element, describe the change

Right-click any element in the live preview. A context menu appears. Describe the change in plain English. The element is replaced server-side using fingerprint matching — deterministic, not regenerated.

Use surgical edits for:
- Changing specific text labels
- Replacing a component with a different EMIS-X component
- Adding or removing a single UI element
- Changing the state of an element (empty state, error state, loading state)

### 3. Vibe Edit — type a free-text instruction in the chat

Type an instruction in the chat panel without right-clicking an element. The agent applies the change via the edit tool.

Use vibe edits for:
- Changes that affect multiple elements ("change all the status badges to use the warning variant")
- Layout changes ("move the filter controls above the list")
- Content changes across the whole prototype ("update all the placeholder patient names to use realistic NHS test data names")

---

## The EMIS-X UI Kit

The prototype builder only uses the EMIS-X UI kit. There is no custom CSS authoring. There is no component library switching.

This is intentional. A prototype that uses non-EMIS-X components creates a false expectation — it looks right in the prototype, then requires rework in P05 when the PxD lead applies the actual design system.

**What the UI kit provides:**
- Typography (headings, body, captions)
- Colour tokens (primary, secondary, semantic — success, warning, error, info)
- Component library (buttons, inputs, tables, lists, badges, navigation, modals, cards)
- Layout patterns (split panel, list-detail, dashboard, form)
- Accessibility compliance (WCAG 2.1 AA) — components are accessible by default

**When prompting for UI elements, use EMIS-X component names:**
- "Add an EMIS-X badge with the warning variant" (not "add a yellow label")
- "Use the EMIS-X data table component" (not "add a table")
- "Apply the EMIS-X primary button style" (not "make the button blue")

If you do not know the component name, describe the intent — the agent knows the UI kit and will select the correct component.

---

## Exercise 1: Generate the Test Prototype

1. Open Genesis AI → Projects → "GP Appointment Reminders (Training)"
2. Click on the P02 stage — Prototype Builder
3. Start a new conversation
4. Type this description:

> "I need a prototype for the GP Appointment Reminder settings screen. A practice manager can see all patients with upcoming appointments in the next 7 days, whether a reminder has been sent, whether the patient has opted out, and whether the reminder bounced (invalid number/email). There should be a way to manually trigger a reminder for a specific patient. Use the EMIS-X data table pattern with a status badge column."

5. Answer the agent's clarifying questions
6. Review the generated prototype in the preview pane

**What to notice:**
- The EMIS-X styling is applied automatically
- The component choices match the EMIS-X design system
- The prototype is interactive — you can click the table rows

---

## Exercise 2: Use a Surgical Edit

1. In the preview, right-click on the "Reminder Status" column header
2. In the context panel, type: "Rename this column to 'Notification Status' and add a tooltip explaining that this shows the last notification attempt"
3. Watch the element update in the preview

**What to notice:**
- Only the targeted element changes — nothing else in the prototype is affected
- The change is immediate — no regeneration of the whole prototype
- The new version is saved to S3 automatically

---

## Exercise 3: Add an Error State

1. In the chat panel, type: "Add an empty state for when no appointments are found in the 7-day window. Use the EMIS-X empty state pattern with an appropriate icon and a message explaining why the list is empty."
2. Review the result

**What to notice:**
- Empty states and error states are as important as the happy path
- The help chat (click `?`) can answer "what is the EMIS-X empty state pattern?" if you are unsure

---

## Prompting for Good Prototypes

### Show don't tell — describe what the user sees, not what they feel
❌ "Make it intuitive and easy to use"
✅ "Show the most urgent unread documents at the top. Use a badge with a count for unread items. Dim documents the user has already actioned."

### Describe the data, not the layout
❌ "Put the patient details on the right"
✅ "When a user clicks a row, show the patient's name, NHS number, appointment date/time, appointment type, and reminder history in a detail panel"

### Reference existing EMIS-X patterns
✅ "Use the same split-panel layout as the EMIS Web clinical task manager"
✅ "The status badges should follow the same colour convention as the EMIS-X inbox — green for received, amber for pending, red for failed"

### Be explicit about states
✅ "Show three states: default (reminder scheduled), sent (green badge, timestamp), and failed (red badge, failure reason)"

---

## Version Recovery

Every time you save changes to the prototype, a new version is created in S3. The previous version is preserved.

To recover a previous version:
1. Click "Recover Version" in the artefact tab
2. Select the version you want to restore
3. Confirm — the selected version becomes the current version

Use this when a vibe edit goes wrong and you want to roll back to a known good state.

---

## Approving the Prototype

Before approving:
- [ ] The prototype correctly represents every user interaction described in the REQ file
- [ ] All primary user flows are visible (not just the happy path)
- [ ] Empty states and error states are present for data-dependent views
- [ ] The UI kit is used correctly — no custom CSS, no non-EMIS-X components
- [ ] A clinical user or BA has reviewed it (even informally — show it in a meeting)

When approved:
- The prototype is stored in S3
- P03 (Architecture) is unblocked
- The PxD lead in P05 uses this as the starting point for the detailed design

---

## Extension: When Figma Integration Lands (Plan 4c Wave H)

When Figma integration is available, you will be able to paste a Figma frame URL into the style reference input. Genesis AI will:
1. Call the Figma API to retrieve the frame as a PNG
2. Feed it into the generation process as a visual reference
3. Generate a prototype that matches the Figma layout

This closes the gap between design intent and prototype output. Until then: describe the layout in plain English and reference existing EMIS-X patterns.

---

## Extension: When Context Graph Lands (Plan KG)

When the Context Graph is live, P02 will have access to every previously approved prototype across all EMIS-X feature repos. When you describe a new feature, the agent will:
- Retrieve similar screens from previous increments
- Apply established EMIS-X interaction patterns automatically
- Flag any deviation from established patterns for review

Prototypes will become faster and more consistent. The EMIS-X design language will compound across every increment.

---

*Genesis AI Training — Module 2 v1.0 | July 2026*
*Next update: when Figma integration (Plan 4c Wave H) and Context Graph (Plan KG) land*
