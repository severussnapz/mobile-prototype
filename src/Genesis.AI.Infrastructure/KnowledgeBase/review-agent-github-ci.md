# Review Agent — GitHub CI Variant

**Activation point:** GitHub Actions, triggered on PR open and push. Runs against the full PR diff. Posts findings as a structured PR review comment. Sets a required status check.

**Base:** `review-agent-base.md` — all base rules, dimensions, finding format, and output structure apply. This document adds Genesis-specific rules scoped to what is available in CI (no runtime artefact context).

**Input available in CI:**
- `git diff` of the PR (all changed files)
- PR title, body, labels (for intent inference)
- The repo's coding standards and anti-shortcut rules (this document)

**Input NOT available in CI** (unlike the Genesis pipeline variant):
- Pinned contract artefacts (not fetched in CI)
- DCB0129 mitigations list (not fetched in CI)
- Runtime seam-test guardrail list

For these, CI defers to the pipeline pre-commit gate (the Genesis pipeline variant). CI is the **safety net**, not the primary gate.

**Output:**
- Structured PR review comment in the finding format (one comment block per finding)
- Required status check:
  - APPROVE / APPROVE WITH COMMENTS → status check passes, PR can merge
  - REQUEST CHANGES / BLOCKED → status check fails, PR is blocked from merge
- On BLOCKED: a summary comment is posted explaining why merge is blocked and what must be fixed

---

## Genesis-Specific Rules (CI-scoped)

All five anti-shortcut rules from the Genesis pipeline variant apply here too, because human-authored code pushed directly to the repo bypasses the P11 pre-commit gate. These are the rules CI uniquely catches for non-AI-generated code.

---

### GENESIS-001 — No Optional/Nullable Dependencies (Critical)

Same as pipeline variant. Every new constructor dependency must be required and throw `ArgumentNullException` on null. Optional/nullable dependencies with fallbacks are blockers.

---

### GENESIS-002 — No Warning Suppression (Critical)

Same as pipeline variant. Any new addition to `NoWarn` in any `Directory.Build.props`, or any new file-scoped `#pragma warning disable` without a documented justification, is a blocker.

---

### GENESIS-003 — No Build-Configuration Edits (Critical)

Same as pipeline variant. Any edit to `Directory.Build.props` not related to a legitimate build property change is a blocker.

---

### GENESIS-004 — No Test Assertion Changes to Force Green (Critical)

Same as pipeline variant. Test assertions, mock setups, and expected values define the contract. Changes to them to make a test pass are blockers.

---

### GENESIS-005 — No Type Erasure in Test Helpers (High)

Same as pipeline variant.

---

### GENESIS-006 — EF Core Conventions (High)

Same as pipeline variant. Every new entity must have explicit `ToTable`/`HasColumnName` mapping for every property.

---

### GENESIS-007 — NHS Data Handling (Critical)

Same as pipeline variant. No NHS numbers, patient identifiers, or special-category personal data in logs, error responses, query strings, or unencrypted fields.

---

### GENESIS-008 — Seam Test Presence (High, CI-scoped)

CI cannot access the full seam-test guardrail list (that requires runtime artefact context). However, CI can statically detect the two most common seam failures from the diff alone:

1. **Result → HTTP body** — if a new field appears in a result type but no corresponding mapping test exists in the diff, flag as high.
2. **Tool registration → wiring** — if a new tool is added to `PipelineToolDefinitions` but no entry appears in `ToolCallWiringTests`, flag as high.

For types 3 and 5 (artefact round-trip, pin resolution), CI defers to the pipeline pre-commit gate — these require runtime context to verify.

---

### GENESIS-009 — Flyway Migration Required (High)

Same as pipeline variant. New entity or schema change without a corresponding `Vnn__description.sql` in `db/migrations/` is a high finding.

---

### GENESIS-010 — Conventional Commits (Medium)

Commit messages must follow the conventional commit format used in this repo:
`type(scope): description`

Types: `feat`, `fix`, `docs`, `test`, `refactor`, `chore`.
Scope: plan reference or workstream (e.g. `plan4c`, `workstream-c`).

A PR whose commits don't follow this format is a medium finding — not a blocker, but flagged for cleanup before merge.

---

## Repo-Type Rules (API — CI-scoped)

- `ProblemDetails` with `userMessage` on every new blocking error path
- No raw exception messages in HTTP responses
- Background failures use the two-tier pattern (`push_failure_log` + `push-status`), never blocking the primary action
- No new endpoints without a controller route test

## Repo-Type Rules (APP — CI-scoped)

- One endpoint per form section, one save handler per button
- Sandbox iframe: production `allow-scripts` only — any broader attribute set is a high finding
- i18n: no hardcoded strings in components — any new user-facing string without a translation key is a medium finding

---


---

### GENESIS-010 — Side-Effect Services Must Not Throw in Constructors (Critical)

Same as pipeline variant. Optional side-effect services must register no-op implementations when configuration is absent, never throw in constructors.

---

### GENESIS-011 — API Client HTTP Verb Must Match Controller HTTP Verb (High)

Any new or modified API client method must use the HTTP verb matching the controller action attribute. CI can catch this statically: if a client method uses `put`/`patch`/`post` and the corresponding controller action in the diff uses a different verb attribute, flag as high. Proved live: `PUT` client calling `[HttpPatch]` controller — 405 in production, zero unit test failures.

---

### GENESIS-012 — Scope-Level Load Must Carry Owning Identifier to Mutation (High)

When data is loaded at a broader scope than the mutation endpoint, the item's owning identifier must be passed to the mutation, not the ambient context identifier. If a mutation handler receives only a context ID (e.g. current conversationId) but the resource was loaded at project scope, flag as high.

---

## CI Integration Notes

**Model:** AWS Bedrock via the existing PrivateLink boundary. No direct Anthropic API calls from CI. The same sovereignty constraint that governs the pipeline governs CI.

**Trigger:** `pull_request` events — `opened`, `synchronize`, `reopened`.

**Output format:** GitHub PR review comment using the finding format from the base prompt. One top-level comment per review run containing the full structured output. Individual inline comments for file:line findings where GitHub's review API supports it.

**Required status check name:** `genesis-review-agent`

**On BLOCKED:** Post a pinned comment summarising the blocking findings. Set the status check to failed. Do not auto-merge or auto-approve under any circumstances.

**Deference to pipeline gate:** If the PR is from `genesis-ai[bot]` (i.e. AI-generated code that already passed the P11 pre-commit gate), CI applies all rules but notes in the summary that the pipeline pre-commit gate already ran. This reduces duplicate noise while maintaining the safety net for any findings the pipeline gate missed.
