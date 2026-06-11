# SKILL: feedback-collection-p05
# Phase: P05 Product Experience Design — Phase 13

## Feedback Collection — Pipeline 05

**Purpose:** Collect structured feedback on the P05 session for continuous improvement.

### Feedback Questions

1. "Was the prototype constraint (if present) applied correctly in Phase 1?"
2. "Were any EMIS UI Kit component mappings wrong or missing?"
3. "Were any wireframes too detailed / not detailed enough?"
4. "Did accessibility requirements surface any design blockers?"
5. "Were empty states or error states ever missed in Phase 9/10?"
6. "What would you change about the Product Experience Design stage for the next iteration?"

### Save Feedback

Append to `feedback/P05_FEEDBACK.md`:

```markdown
# Pipeline 05 Feedback — {DATE}

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

This file is read by `context-loading-p05` on the next iteration start to apply HIGH priority improvements.
