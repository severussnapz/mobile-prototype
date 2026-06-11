# SKILL: context-loading-p03
# Phase: P03 Architecture — Phase 0

## Apply Prior Iteration Learnings

Before anything else, check: does the workspace contain `feedback/ITERATION_REPORT_P03_i*.md`?

- **YES** → Read the most recent file (highest iteration number). Apply all **HIGH** priority prompt improvement recommendations silently. Note **MEDIUM** items as phase-level reminders. Log: `"📋 Prior iteration report P03_i{N} loaded — {X} HIGH priority improvements applied."`
- **NO** → Proceed. This is iteration 1.

## Step 1: Load Pipeline 01/02 Outputs

Announce: "I'll load your requirements from the prior stages. I need manifest.md and all requirements/REQ-*.md files. Ready?"

[Read manifest.md]
[Read all requirement files]

> 🚫 **CODEBASE ISOLATION — MANDATORY:** Load ONLY `manifest.md` and `requirements/REQ-*.md`. Do NOT read any files under `src/`, `tests/`, `db/`, `docs/`, or any other directory. Architecture must be derived exclusively from requirements files and the user interview. Reading existing code biases output toward what already exists rather than what the requirements demand — this is a prompt violation.
>
> **PRECEDENCE NOTE:** PROJECT FOUNDATION content injected by the policy-managed system prompt is permitted and takes precedence over this isolation rule. PROJECT FOUNDATION is a controlled context injection — it is not a codebase file. Only files you load yourself via `get_artefact` are subject to the isolation restriction.

Summarise what was loaded:
```
"I've loaded:
- Product: {PRODUCT_NAME}
- Project Code: {PROJECT_CODE}
- Requirements: {N} total ({X} Must Have, {Y} Should Have)
- Regulatory: {DCB0129/0160 if applicable}
- Genesis AI Guardrails: {CLIN/IG/SEC referenced}

Correct?"
```

## Step 2: Create P03 Review List

Create `feedback/P03_REVIEW_LIST.md` with one row per requirement. Update after each requirement's architecture is confirmed.

```markdown
# Pipeline 03 Review List — {PRODUCT_NAME}
**Started:** {DATE} | **Last Updated:** {DATE}

| REQ-ID | Name | BDAT | ADRs | Failure Modes | Security | Written | Flag | Note |
|---|---|---|---|---|---|---|---|---|
| REQ-001 | {name} | ⏳ | | | | | | |
```

**Key:** `⏳` In progress · `✅` Complete · `↩️` Revised · blank = not started
**Resume rule:** First incomplete, unflagged row = resume point.

## Step 3: Optional Swagger / API Contract Upload

Ask: "Do you have existing API contracts for this product? Upload any Swagger/OpenAPI documents (JSON or YAML) now — or type 'skip' to proceed without them."

**If uploaded:**
1. Parse each document. For every endpoint, extract: HTTP method, path, request body schema, response schemas, and error responses.
2. Build an Existing API Inventory table: `| Method | Path | Request Schema | Success Response | Error Responses | Guardrail Risk |`
3. Apply guardrail checks immediately:
   - ❌ No `[Authorize]` / security scheme declared → flag `AUTH-004 violation`
   - ❌ Response not JSON:API shape (`data.type`, `data.attributes`) → flag `API-001 violation`
   - ❌ Path not versioned (`/api/v1/`) → flag `API-005 violation`
   - ❌ Error responses not using JSON:API `errors[]` → flag `API-007 violation`
   - ❌ No `400`/`422` response for POST/PUT endpoints → flag missing validation response
   - ⚠️ Missing endpoint for a requirement identified in prior stages → flag as **GAP**
4. Accepted endpoints → reference in ADRs; do NOT redesign. Violation endpoints → annotate with required fix (Pipeline 04 carries forward). Gap endpoints → design from scratch in Phase 1.

**If skipped:** Proceed. All API contracts will be designed from requirements in Phase 1.
