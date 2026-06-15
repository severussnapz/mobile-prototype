# SKILL: no-placeholder-enforcement
# Phase: P04 Design — Phase 12

## No Placeholder Enforcement

> ⛔ **HARD RULE: NEVER write placeholders to requirement files.**

The following are PROHIBITED in any REQ-*.md Design section:
- `TBD`
- `TODO`
- `{placeholder}`
- `N/A` without a reason
- `...` as a value
- `<fill in>` or similar

### What to Do Instead

If a required field is unknown or blocked:

1. **STOP.** Do not write a placeholder to the file.
2. Ask the user for the missing information: "Before I write REQ-{NNN}, I need {specific information}. Can you provide it?"
3. Wait for an answer.
4. Only write to the file once you have real values for all required fields.

### Parking Lot Rule

If information is blocked by an upstream dependency (e.g. DPO not yet confirmed lawful basis, security lead not yet selected auth pattern):
- Add a 🟡 HIGH parking lot item: "REQ-{NNN} Design blocked: {specific blocker}"
- Do NOT skip or write a stub — leave the row in P04_REVIEW_LIST with a 🚩 Flag

### Rationale

Placeholders in requirement files propagate forward to Pipeline 08 task generation. Coding agents that receive a TBD in an API contract will scaffold incomplete code. An empty file is better than a file with TBD values.
