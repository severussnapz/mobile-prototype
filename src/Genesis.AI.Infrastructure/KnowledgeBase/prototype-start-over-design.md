# Prototype Demo Builder — Start Over with Context

## Design (July 2026)

### Problem

`handleStartOver` always sends `'Generate a prototype demo'` as a hardcoded string regardless of what the user typed or attached. User instructions and images are discarded. The model starts cold every time.

### Root cause

One line in `handleStartOver`:
```typescript
sendMessage(conversationId, 'Generate a prototype demo', ...)
```

The `instruction` state (the input box) and all attachments are ignored.

---

### Three Start Over modes

**Mode 1 — Fresh start:**
No text typed, no attachments, no existing prototype.
Sends: `'Generate a prototype demo'`
Behaviour: Phase 1 clarifying questions trigger as normal.

**Mode 2 — Directed start:**
User has typed instructions AND/OR attached images/documents. No existing prototype.
Sends: `{typed instructions}` with attachments included.
Behaviour: Phase 1 clarifying questions trigger but use the instructions and attachments as context, producing more focused questions.

**Mode 3 — Rebuild with refinements:**
Prototype already exists (`html` state is non-null). User has typed instructions AND/OR attached images/documents.
Sends: `[REBUILDING] {typed instructions}` with attachments included.
Behaviour: The model detects the `[REBUILDING]` prefix and skips generic clarifying questions, generating directly from the instructions and attachments.

**In all modes:** attachments are always included. They are NOT cleared after Start Over fires. The user removes them manually by clicking the X on each chip.

---

### [REBUILDING] prefix — display rule

The `[REBUILDING] ` prefix is sent to the model but stripped from the chat display. The user sees their typed instructions exactly as written. No internal signal is ever visible in the chat panel.

Implementation: in the message rendering logic, strip any message starting with `[REBUILDING] ` before displaying the user turn.

---

### Anchor failure handling

Raw `ANCHOR_NOT_FOUND` and `ANCHOR_AMBIGUOUS` error codes are never shown to the user.

Replace with:
> "I couldn't apply that edit precisely. Try rephrasing the instruction, or click Start Over to rebuild with updated instructions."

Translation key: `PrototypeDemo.AnchorFailure`

---

### Version recovery as safety net

The previous prototype version is always saved automatically before Start Over fires. If the rebuild produces a worse result, the user restores via **Recover Version**. This removes the risk from Start Over — it is always reversible.

---

### What does NOT change

- Phase 1 clarifying questions still run in Mode 1 and 2
- Conversation history is cleared on Start Over (clean context for the model)
- All existing save guards apply to the rebuild (`<!DOCTYPE html>`, `PROTOTYPE ONLY` banner, NHS number check)
- Token usage is recorded for the rebuild generation

---

### Implementation

**API — no changes needed.**
`sendMessage` already accepts the message as a string. `PrototypeSingleFileEnabled` already handles prompt and tool selection. The model prompt already handles the `[REBUILDING]` signal via the skill file.

**App changes:**

1. `handleStartOver` in `PrototypeDemoPage.tsx`:

```typescript
const handleStartOver = useCallback((): void => {
  if (!conversationId) return;
  setGenerateError(null);
  setSelectedOuterHtml(null);
  setIsGenerating(true);

  const messageText = instruction.trim() || t('PrototypeDemo.DefaultGenerateMessage');
  const prefix = html ? '[REBUILDING] ' : '';

  sendMessage(
    conversationId,
    `${prefix}${messageText}`,
    attachedImages.length ? attachedImages : undefined,
    attachedDocuments.length ? attachedDocuments : undefined,
  );

  setInstruction(''); // clear typed input only — attachments persist
  // Do NOT call setAttachedImages([]) or setAttachedDocuments([])
}, [attachedDocuments, attachedImages, conversationId, html, instruction, sendMessage, t]);
```

2. Message display — strip `[REBUILDING] ` prefix before rendering user messages in the chat panel.

3. Anchor failure display — replace raw error codes with `t('PrototypeDemo.AnchorFailure')`.

4. Translation keys to add:
```json
"DefaultGenerateMessage": "Generate a prototype demo",
"AnchorFailure": "I couldn't apply that edit precisely. Try rephrasing the instruction, or click Start Over to rebuild with updated instructions."
```

**API prompt — `PrototypeDemoGeneration.md` skill file:**
Add handling for the `[REBUILDING]` prefix in Phase 1:

> If the user message begins with `[REBUILDING]`, skip Phase 1 clarifying questions entirely. Extract the instructions after the prefix and proceed directly to prototype generation. The user has explicitly provided their brief — do not ask questions they have already answered.

---

### Dependency

The `[REBUILDING]` prefix handling requires a prompt change to `PrototypeDemoGeneration.md`. This goes through the standard CODEOWNERS PR process — `@emisgroup/clinical-safety-owners` approval is not required for the prototype generation prompt (P02), but the standard review gate applies.
