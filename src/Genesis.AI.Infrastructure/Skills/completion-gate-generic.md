---
name: completion-gate-generic
description: 'Use this skill in all pipeline stages. Defines the canonical requirement completion event format, the pre-write gate checklist, and the handoff status block that downstream stages consume.'
metadata:
  version: 1.0.0
  applyTo:
    - requirements
---

# Completion Gate (Generic)

## Purpose

The completion gate is the final check before a requirement is marked as complete for a stage.
It prevents incomplete or placeholder-containing artefacts from advancing to normalisation.

## Pre-Write Gate — 6 Checks

Run these checks before writing the final artefacts. If any check fails, stop and resolve before writing.

| Check | Pass condition |
|-------|---------------|
| A — All required sections present | Every mandatory section in the stage output template exists and is non-empty |
| B — No placeholder content | No `[TBD]`, `[TODO]`, `[PLACEHOLDER]`, or equivalent strings in any artefact |
| C — Parking lot resolved | All CRITICAL items resolved; all HIGH items either resolved or explicitly accepted as carry-forward |
| D — Consistency with upstream | No contradiction of locked entries from `feedback/VALUE_CHAIN.md` |
| E — Guardrail coverage claimed | All applicable guardrails cited; none omitted without explicit justification |
| F — Human decisions confirmed | All phases requiring human confirmation (CSO, DPO, reviewer) have documented responses |

## Check Failure Protocol

If any check fails:
1. State which check failed and why.
2. Do not write artefacts.
3. Add a CRITICAL parking lot item describing the failure.
4. Ask the human how to proceed.

## Canonical Requirement Completion Event

When all 6 checks pass, emit this completion event in your response before writing artefacts:

```
✅ COMPLETION GATE PASSED — REQ-[NNN]
Stage: [stage code]
Checks: A✅ B✅ C✅ D✅ E✅ F✅
Writing artefacts now.
```

Then emit the `save_artefact` tool calls.

## Handoff Status Block

After artefacts are written, append the handoff status to the iteration report:

```
HANDOFF: REQ-[NNN] → [next stage code]
Status: READY
Iteration: [N]
Completion gate: ALL PASSED
Key outputs: [comma-separated artefact list]
```

If any check was waived (human decision), the status must be:
```
Status: READY-WITH-WAIVERS
Waivers: [list of waived checks and justifications]
```
