---
name: iteration-report
description: 'Use this skill in all pipeline stages at the final phase. Defines the iteration report scoring template, mandatory generation rule, and handoff status block format.'
metadata:
  version: 1.0.0
  applyTo:
    - requirements
---

# Iteration Report

## Mandatory generation rule

Every stage conversation MUST produce an iteration report at the final phase.
The iteration report is not optional. A stage that produces artefacts without an iteration report is incomplete.

Generate the report using the `save_artefact` tool to write `feedback/[STAGE_CODE]_ITERATION_REPORT_REQ[NNN].md`.

## Scoring Template

```markdown
# [Stage Name] Iteration Report — REQ-[NNN] — Iteration [N]

**Date:** [ISO date]
**Stage:** [stage code]
**Requirement:** [REQ identifier and short title]
**Iteration:** [N] (previous iterations: [list or "none"])

---

## Output Quality

| Dimension | Score (1–5) | Notes |
|-----------|-------------|-------|
| Completeness — all required sections present | [N] | |
| Depth — no placeholder content | [N] | |
| Consistency with prior stages | [N] | |
| Guardrail coverage | [N] | |
| Parking lot resolution rate | [N] | |

**Overall:** [N/5]

---

## Artefacts Produced

| File | Version | Status |
|------|---------|--------|
| [path] | v[N] | new / updated |

---

## Open Parking Lot Items

| ID | Priority | Content | Age (turns) |
|----|----------|---------|-------------|
| [id] | [priority] | [content] | [N] |

---

## Gaps and Outstanding Actions

[Explicit list of anything not resolved in this iteration. Empty section = no known gaps.]

---

## Handoff Status

**Ready for downstream:** [YES / NO — with reason if NO]

If YES:
```
HANDOFF: REQ-[NNN] → [next stage code]
Status: READY
Iteration: [N]
Key outputs: [comma-separated artefact list]
```
```

## Score Interpretation

| Score | Meaning |
|-------|---------|
| 5 | Complete, no gaps, no placeholders |
| 4 | Complete with minor advisory gaps |
| 3 | Incomplete in one area — flag for review |
| 2 | Significant gaps — do not advance without resolution |
| 1 | Cannot be used as-is — requires full re-run |

A score of 2 or below in any dimension MUST produce a CRITICAL parking lot item and a human review flag before handoff.
