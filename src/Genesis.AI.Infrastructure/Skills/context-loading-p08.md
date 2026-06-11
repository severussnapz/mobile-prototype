# SKILL: context-loading-p08
# Phase: P08 Security — Phase 0

## Apply Prior Iteration Learnings

Check: does the workspace contain `feedback/ITERATION_REPORT_P08_i*.md`?
- **YES** → Read the most recent. Apply all HIGH priority improvements silently.
- **NO** → Proceed. This is iteration 1.

## Step 1: Load P07 Handoff Notes

Check for `feedback/P07_P08_HANDOFF.md`. If it exists: load it. Apply the "P08 must address" items as mandatory tasks in this session.

> **PRECEDENCE NOTE:** PROJECT FOUNDATION content is pre-loaded — do NOT reload those files.

## Step 2: Load P04 Design and P03 Architecture

Load `manifest.md` and all `requirements/REQ-*.md`. Focus on:
- `## Architecture (Added by Pipeline 03)` — technology stack, service classification
- `## Design (Added by Pipeline 04)` — API contracts, component interfaces

Summarise: "I've loaded {N} requirements. P07 handoff: {found/not found}."

## Step 3: Security Framing Check

From ROUTING CONTEXT: `security_framing_present`.
- `true` → P03 included a `### Security Framing` section. Load it. Use it as the baseline threat model. Phases 1 and 2 delta only.
- `false` → Full threat model in Phases 1 and 2.

## Step 4: Create P08 Review List

Create `feedback/P08_REVIEW_LIST.md`:

```markdown
# Pipeline 08 Security Review List — {PRODUCT_NAME}
**Started:** {DATE}

| REQ-ID | Name | Threats | Controls | OWASP | ASVS | Written | Flag |
|--------|------|---------|---------|-------|------|---------|------|
| REQ-001 | {name} | ⏳ | | | | | |
```
