# SKILL: cso-review-final
# Phase: P06 Clinical Safety — Phase 13

## CSO Final Review

**Purpose:** Final CSO review of the completed clinical safety documentation.

### Final Review Questions

1. "Is the clinical safety case complete and accurate?"
2. "Are there any hazards that should be elevated to HIGH?"
3. "Any additional controls you'd like to add before sign-off?"
4. "Is the DCB0129 evidence trail sufficient for regulatory submission?"
5. "Shall I generate the formal clinical safety summary report?"

### Clinical Safety Summary Report

If CSO confirms ready: create `artefacts/clinical-safety/CLINICAL_SAFETY_SUMMARY.md`:

```markdown
# Clinical Safety Summary — {PRODUCT_NAME}
**DCB0129 Reference:** {VERSION}
**Date:** {DATE}
**CSO:** {Name} — {Role}

## Executive Summary
{N} hazards identified across {M} requirements.
Highest residual risk: {Level} (HAZ-NNN).
All controls confirmed. CSO approved.

## Hazard Summary
| HAZ ID | Description | Residual Risk | Status |
|--------|------------|--------------|--------|
| HAZ-001 | ... | LOW | Controlled |

## Controls Summary
| C-NNN | Type | Description | Status |
|-------|------|-------------|--------|
| C-001 | PREVENT | ... | Implemented |
```

### Feedback Collection

Ask: "What would you improve about the Clinical Safety stage for the next product?"
Save to `feedback/P06_FEEDBACK.md`.
