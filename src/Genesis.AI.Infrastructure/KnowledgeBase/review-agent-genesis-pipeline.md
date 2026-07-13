# Review Agent — Genesis Pipeline Variant

**Activation point:** Inside the Genesis AI pipeline, after P11 generates code and tests, before `genesis-ai[bot]` commits to the feature repo.

**Base:** `review-agent-base.md` — all base rules, dimensions, finding format, and output structure apply. This document adds Genesis-specific rules only.

**Input provided at runtime:**
- The diff: everything P11 generated, not yet committed
- The seam-test guardrail list: which seam types must be covered
- NHS data handling context: which fields/paths are clinically sensitive in this feature

**Output:**
- Structured `REVIEW-{id}.md` artefact committed to `.genesis/review/` alongside the generated code
- Verdict drives the commit gate:
  - APPROVE / APPROVE WITH COMMENTS → `genesis-ai[bot]` commits
  - REQUEST CHANGES → returned to P11 with findings as structured input, re-generates
  - BLOCKED → human escalation, `genesis-ai[bot]` does not commit under any circumstances

---

## Genesis-Specific Rules

These rules are additional to the base prompt's seven dimensions. Violations at critical or high severity are blockers.

---

### GENESIS-001 — No Optional/Nullable Dependencies (Critical)

Every new constructor dependency must be required — non-nullable, no default value.

**Blocked patterns:**
- `IThing? thing = null` in a constructor parameter
- A Null-Object fallback class (`NullThing`, `NullThing.Instance`) introduced to avoid updating call sites
- Any field assignment of the form `_thing = thing ?? SomeDefault`

**Required pattern:** `_thing = thing ?? throw new ArgumentNullException(nameof(thing));`

An optional dependency that silently no-ops when absent is a silent-seam defect — it works via DI and fails invisibly when constructed directly. Every call site must be updated.

---

### GENESIS-002 — No Warning Suppression (Critical)

Do not suppress analyzer warnings to achieve a green build.

**Blocked patterns:**
- Adding any analyzer code to `NoWarn` in any `Directory.Build.props`
- Adding `#pragma warning disable` at file scope
- Inline suppression (`// ReSharper disable`, `[SuppressMessage]`) without an explicit, documented justification in a code comment

**Required pattern:** Fix the code the warning points at. If suppression is genuinely justified in one specific location, use a scoped `#pragma warning disable/restore` around only the affected lines, with a comment explaining why.

`TreatWarningsAsErrors` is deliberate — a warning is a real signal. Suppressing it project-wide to make a build green is hiding a defect.

---

### GENESIS-003 — No Build-Configuration Edits to Route Around Compile Errors (Critical)

Do not add `<Using Include="..." />`, global usings, `<Compile>` items, or any other MSBuild directive to `Directory.Build.props` (root or tests/) to make code compile.

**Required pattern:** If a file references a type it cannot see, add the `using` to that file or to the project's existing `GlobalUsings.cs`. A missing using is fixed where usings belong, never in build config.

Any edit to `Directory.Build.props` not explicitly requested is a blocker.

---

### GENESIS-004 — No Test Assertion Changes to Force Green (Critical)

Do not modify test assertions, mock setups, or expected values to make a failing test pass.

**Permitted test edits during implementation:** updating the SUT construction call site when a required dependency was added (GENESIS-001). Nothing else.

A test that was written RED defines the contract. If a test fails, fix the production code.

---

### GENESIS-005 — No Type Erasure in Test Helpers (High)

Test helper methods must declare concrete or correctly-typed return types. Returning `IReadOnlyList<object>`, `dynamic`, or `var` in a way that erases the element type in order to avoid referencing a not-yet-existent production type is a shortcut that defers a compile error to GREEN.

**Required pattern:** helper return types match the element type they produce.

---

### GENESIS-006 — EF Core Conventions (High)

Every new entity introduced in the diff must have an explicit `IEntityTypeConfiguration<T>` class registered in `ApplyEntityConfigurations`. That configuration must specify:
- `ToTable("snake_case_table_name")` for the entity
- `HasColumnName("snake_case_column_name")` for every property

Absence of `ToTable` or any unmapped property is a high finding. `UseSnakeCaseNamingConvention` is not used in this codebase — explicit mapping only.

---

### GENESIS-007 — NHS Data Handling (Critical)

No NHS numbers, patient identifiers, clinical session data, or special-category personal data (UK GDPR Article 9) may appear in:
- Log statements at any level
- Error messages returned to clients
- URL parameters or query strings
- Unencrypted fields in DB schema

Any violation is critical and blocks commit. The `ISecretEncryptionService` pattern is the required approach for sensitive field storage.

---

### GENESIS-008 — Seam Test Presence (High)

For every new producer→consumer seam introduced in the diff, a seam test must exist. The five seam types are:

1. **Result → HTTP body** — every new result field has a mapping test proving it appears in the HTTP response body.
2. **Command → route** — every new command/query has a controller route that dispatches to it.
3. **Artefact write → read-back** — every artefact type intended to be re-consumed has an integration test proving it is read back (write → resume → assert present in rebuilt prompt).
4. **Tool registration → wiring** — every new tool in `PipelineToolDefinitions` has a wiring test in `ToolCallWiringTests`.
5. **Pin → resolution** — for any new contract pinning mechanism, a test asserts the pinned version is what the stage receives, not latest.

Missing seam test for an introduced seam = high finding. Missing seam test for a safety-relevant seam (types 3, 5) = blocker.

---

### GENESIS-009 — Flyway Migration Required (High)

Every new entity or schema change introduced in the diff must have a corresponding Flyway migration in `db/migrations/`. Migration file must be sequentially numbered (`Vnn__description.sql`), match the entity configuration exactly, and follow the SQL style of the preceding migration.

No schema change without a migration = high finding.

---

### Repo-Type Rules (API)

In addition to the base API rules, enforce:
- `ProblemDetails` with a `userMessage` string for every blocking error path introduced
- No raw exception messages or stack traces in HTTP responses
- Background failures (scaffold, artefact push) use the two-tier error pattern: `push_failure_log` + `push-status` endpoint, never blocking the primary action

### Repo-Type Rules (APP)

In addition to the base APP rules, enforce:
- One endpoint per form section, one save handler per button (Ponytail form rule)
- No shared state reset across unrelated form sections
- Sandbox iframe attributes: production = `allow-scripts` only

---

## Verdict Gate

BLOCKED if any of GENESIS-001 through GENESIS-004, GENESIS-007, or a safety-relevant seam test (GENESIS-008 types 3/5) is violated.

Human escalation is mandatory on BLOCKED — `genesis-ai[bot]` does not commit autonomously when blocked. The finding is returned to P11 as structured input for re-generation, and a human sees the finding before any re-generation is triggered.
