---
name: carry-forward-contract
description: 'Use this skill in all pipeline stages. Defines the VALUE_CHAIN append format, what consumed/added/must-preserve/gaps sections must contain, and the rule against silent omission.'
metadata:
  version: 1.0.0
  applyTo:
    - requirements
---

# Carry-Forward Contract

## Purpose

The carry-forward contract ensures that each stage hands off a complete, coherent context to the next stage. Nothing established in prior stages should be silently lost or overwritten.

## VALUE_CHAIN Format

At the end of each stage, append to `feedback/VALUE_CHAIN.md` using this structure:

```markdown
## [Stage Name] — REQ-[NNN] — [Date]

### Consumed from prior stages
- [What this stage read from upstream artefacts]
- [Which decisions were taken as inputs, not re-derived]

### Added by this stage
- [New artefacts produced]
- [New decisions made]
- [New constraints established]

### Must preserve for downstream stages
- [Facts downstream stages MUST NOT contradict]
- [Decisions that are now locked — no downstream re-derivation]

### Gaps identified
- [Questions that arose but were not resolved in this stage]
- [Items parked for downstream stages to resolve]
```

## Rule 1 — No silent omission

If a prior stage established a decision, the next stage must either:
- Explicitly reference it as consumed input, or
- Explicitly state why it is being revised (and add a HIGH parking lot item)

A downstream stage that produces output contradicting a locked upstream decision without annotation is a pipeline error.

## Rule 2 — Must-preserve entries are locked

Entries under "Must preserve for downstream stages" are locked.
A downstream stage cannot re-derive or contradict them without:
1. Explicitly stating the conflict.
2. Adding a CRITICAL parking lot item.
3. Waiting for human resolution.

## Rule 3 — Gaps are not failures

Gaps are expected and normal. An empty "Gaps identified" section does not mean the stage is complete — it means no gaps were identified. That is a claim that should be explicitly verified.

## Rule 4 — Fetch fresh before appending

Always call `get_artefact` on `feedback/VALUE_CHAIN.md` before appending.
Never overwrite the file. Append only. The file is the cumulative cross-stage audit trail.
