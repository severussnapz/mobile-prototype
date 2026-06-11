# SKILL: context-loading-p04
# Phase: P04 Design — Phase 0

## Apply Prior Iteration Learnings

Check: does the workspace contain `feedback/ITERATION_REPORT_P04_i*.md`?

- **YES** → Read the most recent file. Apply all HIGH priority improvements silently. Log: `"📋 Prior iteration report P04_i{N} loaded — {X} HIGH priority improvements applied."`
- **NO** → Proceed. This is iteration 1.

## Step 1: Load P03 Architecture Outputs

Load `manifest.md` and all `requirements/REQ-*.md` files. Focus on the `## Architecture (Added by Pipeline 03)` sections.

> 🚫 **CODEBASE ISOLATION — MANDATORY:** Load ONLY `manifest.md` and `requirements/REQ-*.md`. Do NOT read source code files. Design must be derived from requirements and architecture sections only.

> **PRECEDENCE NOTE:** PROJECT FOUNDATION content is pre-loaded — do NOT reload those files via `get_artefact`.

Summarise: "I've loaded {N} requirements. Architecture sections present for {M}/{N}."

## Step 2: Check for Existing Swagger Contracts

If ROUTING CONTEXT shows `swagger_present: true`:
- Swagger was uploaded and processed in P03. The API Inventory is in the Architecture sections.
- In Phase 1: take accepted Swagger endpoints as authoritative contracts. Run annotation pass for violations only. Design gap endpoints from scratch.

If `swagger_present: false`: full API contract design in Phase 1.

## Step 3: Create P04 Review List

Create `feedback/P04_REVIEW_LIST.md`:

```markdown
# Pipeline 04 Review List — {PRODUCT_NAME}
**Started:** {DATE} | **Last Updated:** {DATE}

| REQ-ID | Name | API | DB | Interfaces | State | Validation | Written | Flag | Note |
|--------|------|-----|----|-----------|-------|-----------|---------|------|------|
| REQ-001 | {name} | ⏳ | | | | | | | |
```

**Resume rule:** First row with blank Written and no Flag = resume point.
