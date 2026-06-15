# SKILL: feedback-collection-p03
# Phase: P03 Architecture — Phase 13

## Feedback Collection — Pipeline 03

**Purpose:** Collect structured feedback on the P03 session for continuous improvement of the pipeline prompt.

### Feedback Questions

Ask after the iteration report is generated:

1. "Were any phases too slow? Which ones and why?"
2. "Were any questions redundant (already covered by prior stages)?"
3. "Were any required questions missing from the interview?"
4. "Did the routing context reduce unnecessary work, or did it still ask things that were already known?"
5. "Were the ADR templates useful as-is, or did they need modification?"
6. "Any security framing gaps that Pipeline 08 later had to re-derive?"
7. "What would you change about the Architecture stage for the next iteration?"

### Save Feedback

Append responses to `feedback/P03_FEEDBACK.md`:

```markdown
# Pipeline 03 Feedback — {DATE}

## Session: {PROJECT_CODE} Iteration {N}

### What worked well
{Responses}

### What to improve
{Responses}

### Prompt improvement recommendations
- HIGH: {Specific change needed}
- MEDIUM: {Specific change needed}
- LOW: {Specific change needed}
```

This file is read by `context-loading-p03` on the next iteration start to apply HIGH priority improvements.
