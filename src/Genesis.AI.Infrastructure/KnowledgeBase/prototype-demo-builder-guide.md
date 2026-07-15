# Prototype Demo Builder — User Guide

## What it is

The Prototype Demo Builder generates a clickable HTML prototype directly from your requirements. It replaces hand-drawn wireframes and static mockups with a live, interactive preview you can share with stakeholders and refine in real time. Everything runs inside Genesis AI — nothing leaves the VPC.

---

## The layout

The builder has two panels side by side:

**Left — Chat panel.** This is where you describe what you want, ask for changes, and see the build progress. It works like a conversation. Below the chat is the input box where you type your instructions.

**Right — Preview panel.** This shows the live prototype rendered in a sandboxed iframe. As soon as the model saves a new version, the preview updates automatically. You can also switch to Code view to see the raw HTML.

The divider between the two panels is draggable. Grab it and pull left or right to give more space to whichever panel you need. If you prefer keyboard, tab to the divider and use the left and right arrow keys.

---

## The buttons

**Start Over** — clears the conversation and generates a fresh prototype using whatever instructions and attachments are currently in the input area. If the input box is empty and no attachments are present, the model asks clarifying questions before generating. If you have typed instructions or uploaded images, those are sent as the starting brief. Use Start Over when the overall layout or structure needs to change, not for small tweaks.

**Open full screen** — opens the prototype in a new browser tab at full size. Useful for stakeholder reviews.

**Preview / Code toggle** — switches the right panel between the rendered preview and the raw HTML source.

**Stop** — cancels the current generation mid-stream. The prototype may be incomplete if you stop early.

**Recover Version** — opens a list of all saved versions of the prototype. Click any version to restore it. Use this if Start Over produces something you liked less than the previous version — you can always go back.

---

## Building your first prototype

1. Open the Prototype Demo Builder from the project page.
2. Type a description of what you want in the input box. Be specific — mention the feature, the key screens, the layout, and any requirements you want reflected. Example:

   > "Build a prototype for the unified inbound inbox from REQ-001 and REQ-002. The inbox should have a list view on the left with patient name, document type, and date received. Clicking a row opens the document preview on the right with filing and triage actions at the top."

3. Optionally attach images — screenshots of the current EMIS Web screen, a hand-drawn sketch, or a Figma export. The model will use them as visual reference alongside your text.
4. Click **Start Over** or press Enter to send.
5. The model may ask a few clarifying questions before generating. Answer them to get a more accurate result.
6. The preview updates automatically when the prototype is ready.

---

## Uploading images and documents

You can attach images and documents to give the model visual and written reference. Supported formats: PNG, JPG, PDF, Markdown.

Click the attach button (paperclip icon) in the input area, or drag and drop files onto the input box. Attachments appear as chips above the input. They persist across Start Over — you do not need to re-attach them each time. Remove an attachment by clicking the X on its chip.

Good uses for image attachments:
- A screenshot of the current EMIS Web screen you are replacing
- A hand-drawn sketch of the layout you want
- A Figma export of the design direction
- A competitor screen you want to reference

Good uses for document attachments:
- A requirements document in Markdown or PDF
- An existing design brief
- A set of acceptance criteria

The model reads all attachments as part of its context. Be explicit in your text about what you want it to take from each attachment — "use this layout but with the EMIS-X UI kit" or "match the column structure in this screenshot".

---

## Making small changes — vibe edits

For small changes (colours, text, layout of one element, adding a button), type your instruction in the input box and press Enter. The model edits the existing prototype in place. Examples:

- "Change the header to use the EMIS teal colour"
- "Add a 'Mark as urgent' button next to the file button"
- "Move the patient name to the top of the card"
- "Make the table sortable by date received"

The preview updates as soon as the edit is applied. If the change looks right, continue. If not, describe what you want differently and the model will try again.

---

## Making targeted edits — surgical edits

For precise element-level changes, right-click on any element in the preview. A context menu appears. Select the element and type your instruction. The model returns an updated version of exactly that element without touching anything else. Examples:

- Right-click on a button → "Change the label to 'File to record'"
- Right-click on a table row → "Add a status badge column"
- Right-click on a heading → "Make this an h2 and add a subtitle underneath"

Surgical edits are faster and more reliable than vibe edits for precise changes because the model only sees the element you selected, not the entire prototype.

If a surgical edit cannot be applied, you will see a message: "I couldn't apply that edit precisely. Try rephrasing the instruction, or click Start Over to rebuild with updated instructions." This happens when the model changes the element enough that the system cannot reliably match it back to its original position in the HTML.

---

## Making large changes — when to Start Over

Use Start Over when:
- The overall layout needs restructuring
- You want to add a completely different section or page
- You have done several edits and the prototype has drifted from what you wanted
- A surgical edit keeps failing and rephrasing is not helping

Before clicking Start Over, type your updated instructions in the input box. Include everything you want the new version to have — the model does not carry forward memory of what was in the previous version. Be specific:

> "Rebuild the inbox with a three-column layout — navigation on the far left, document list in the centre, document preview on the right. Based on REQ-001, REQ-002, and REQ-003. Keep the triage actions from the previous version but move them into the preview column header."

You can also attach images alongside your rebuild instructions — for example, a new sketch of the layout you want. Attachments persist across Start Over so any images already attached will be included automatically.

The previous version is always saved automatically before Start Over runs. If the new version is worse, use **Recover Version** to restore it.

---

## How Start Over uses your context

Start Over behaves differently depending on what is in the input area and whether a prototype already exists:

**No input, no prototype:** The model asks clarifying questions before generating. This is the default first-build flow.

**Instructions typed (and/or images attached), no prototype:** The model uses your input as the brief for its clarifying questions, producing more focused and relevant questions before generating.

**Instructions typed (and/or images attached), prototype already exists:** The model knows it is rebuilding. It skips generic clarifying questions and generates directly using your instructions and attachments. The more detail you provide, the more closely the rebuild will match your intent.

In all cases, attachments are always included. They persist until you remove them manually.

The `[REBUILDING]` signal is sent internally to the model — you will not see it in the chat. Your instructions appear in the chat exactly as you typed them.

---

## Version history

Every time the model saves a new prototype, a version is created automatically. You can browse and restore any previous version using the **Recover Version** button.

Version history is stored in S3. Versions are not deleted when you start over — you can always go back to any point in the build history.

Use version history as your safety net. Start Over is always reversible.

---

## Parking lot, Notes & Decisions

These panels on the left side of the chat capture things that come up during the prototype session but are not edits to the prototype itself.

**Parking lot** — items flagged during the session that need follow-up. The model adds items here when it identifies something that cannot be resolved in the prototype (e.g. a design decision that needs a stakeholder call, a requirement that is ambiguous).

**Notes & Decisions** — key decisions made during the session. Useful for recording why a particular layout was chosen.

Both panels persist across sessions and are visible to anyone with access to the project.

---

## What the prototype is not

The prototype is a **requirements validation artefact** — it is not production UI and not a design specification. Its purpose is to make requirements tangible so stakeholders can validate them before engineering begins. The "PROTOTYPE ONLY" banner in the preview is mandatory and cannot be removed.

The prototype is built with the EMIS-X UI kit. It uses real EMIS-X components and design tokens so it looks and feels like the real product, but it is static HTML — there is no backend, no data, and no real functionality.

---

## Tips

- Be specific in your instructions. "Make it look better" produces worse results than "increase the padding between rows and use a lighter background on alternate rows".
- Reference requirement IDs (REQ-001, REQ-002) in your instructions. The model has access to all approved artefacts in the project and will use them as context.
- Attach a screenshot of the current EMIS Web screen when building a migration prototype. The model will use it to understand the existing workflow.
- Use surgical edits for element-level changes and Start Over for structural changes. Trying to vibe-edit a large structural change often produces unpredictable results.
- Attachments persist across Start Over. Load up your reference images once and they stay available for every rebuild.
- If the model asks clarifying questions you have already answered, type "proceed with the instructions I gave" and it will generate without further questions.
- Use version history as your safety net. Start Over is reversible.
