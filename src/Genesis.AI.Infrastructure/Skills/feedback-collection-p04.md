# SKILL: feedback-collection-p04
# Phase: P04 Design — Phase 13

## Feedback Collection — Pipeline 04

**Purpose:** Collect structured feedback on the P04 session for continuous improvement.

### Feedback Questions

1. "Were any Phase 0B routing decisions wrong?" → Did existing_use/extend/modify classifications match reality?
2. "Were any API contract questions redundant (already decided in P03)?"
3. "Were any DDL patterns missing from the database schema skill?"
4. "Did any state machines need more transitions than expected?"
5. "Were placeholder blockers encountered? What caused them?"
6. "What would you change about the Design stage for the next iteration?"

### Save Feedback

Append to `feedback/P04_FEEDBACK.md`:

```markdown
# Pipeline 04 Feedback — {DATE}

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

This file is read by `context-loading-p04` on the next iteration start to apply HIGH priority improvements.
