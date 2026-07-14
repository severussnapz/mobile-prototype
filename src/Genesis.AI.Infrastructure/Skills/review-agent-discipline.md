# Skill: Review Agent Discipline
**Stage:** Review Agent — pre-commit (P11 gate) and GitHub CI (PR gate)
**Injection:** All phases of the review agent

---

## REV-001 — Evidence-Based Findings Only (Guardrail)

**Severity:** Critical

Every finding must be grounded in the diff. No speculative findings, no "this might be a problem", no invented files or tests.

**Required for every finding:**
- The exact file path and line range
- A quoted or precisely-described snippet showing the issue
- Why it matters — the concrete consequence, not a general principle
- A specific fix — the actual code or pattern change required

If you are not certain a finding is real: say "Needs human verification" and state exactly what to verify. Do not lower your confidence and still report the finding as if it were certain.

---

## REV-002 — Anti-Shortcut Checks Are Blockers (Guardrail)

**Severity:** Critical

The following patterns in the diff are BLOCKED findings. They go green while hiding a defect and must be flagged on every review regardless of other findings.

**Check 1 — Optional/nullable dependencies:**
Grep every new constructor for `? = null` parameters and any new class matching `Null{TypeName}`, `Default{TypeName}`, or containing `Instance` that returns an empty/no-op version of a type. An optional dependency that silently no-ops when absent is a bypass.
```csharp
❌ ISessionCloseContextBuilder? sessionCloseContextBuilder = null
❌ _builder = sessionCloseContextBuilder ?? NullSessionCloseContextBuilder.Instance;
✅ _builder = sessionCloseContextBuilder ?? throw new ArgumentNullException(nameof(sessionCloseContextBuilder));
```

**Check 2 — Warning suppression:**
Any addition to `NoWarn` in any `Directory.Build.props`, any new file-scope `#pragma warning disable`, or any new `[SuppressMessage]` without a documented justification comment is a blocker. `TreatWarningsAsErrors` is deliberate — the warning is real signal.

**Check 3 — Build-configuration edits:**
Any new `<Using Include="..." />`, global using, `<Compile>` item, or other MSBuild directive added to `Directory.Build.props` to make code compile is a blocker. A missing using is fixed in the file or in `GlobalUsings.cs` — never in build config.

**Check 4 — Test assertion changes:**
Any modification to an existing test assertion, mock return value, or expected output is a blocker unless the change is solely updating an SUT constructor call to add a required dependency. Tests define the contract; changes require justification.

**Check 5 — Type erasure in test helpers:**
Any helper method returning `IReadOnlyList<object>`, `dynamic`, or an erased type when it produces concrete domain entities is a blocker.

---

## REV-003 — Seam Completeness Check (Guardrail)

**Severity:** High

For every new field on a result type, check the diff contains: (a) the response DTO mapping, (b) the controller mapping, (c) a test asserting the field appears in the HTTP response. Any of the three missing = high finding. All three missing = blocker.

For every new tool in `PipelineToolDefinitions`, check the diff contains a wiring test in `ToolCallWiringTests`. Missing = high.

For every new artefact type declared as re-consumed, check the diff contains an integration test proving write→read-back. Missing = blocker.

---

## REV-004 — NHS Data Handling (Guardrail)

**Severity:** Critical

Scan every new log statement, error message, URL construction, and DB column in the diff for patient identifiers, NHS numbers, or Article 9 special-category data. Any such data in any of these surfaces is a Critical finding and a blocker.

NHS number format: `\d{3}\s?\d{3}\s?\d{4}` — flag any value matching this pattern in a log or error context.

Fictional data in tests must use obviously-fake identifiers (`NHS: XXXX`, `Patient-001`) — never format-plausible values.

---

## REV-005 — EF Core Mapping Completeness (Guardrail)

**Severity:** High

For every new entity class in the diff: verify an `IEntityTypeConfiguration<T>` class exists in the same diff and is registered in `ApplyEntityConfigurations`. Verify `ToTable("snake_case")` and `HasColumnName("snake_case")` are present for every property. Any unmapped property or missing configuration is a high finding.

`UseSnakeCaseNamingConvention` is not used in this codebase — explicit mapping only.

---

## REV-006 — Migration Required (Guardrail)

**Severity:** High

Any new entity class or schema change in the diff must be accompanied by a Flyway migration in `db/migrations/Vnn__description.sql`. The migration must be sequentially numbered (verify against the last existing file) and must match the entity configuration exactly. No schema change without a migration = high finding.

---

## REV-007 — Severity-Tag Every Finding (Steer)

**Severity:** High

Every finding is tagged: **blocker** / **high** / **medium** / **low**. Do not produce a wall of undifferentiated comments. The author must be able to read the verdict in one pass and know exactly what gates the merge and what is optional.

Blockers: any REV-002 through REV-004 violation; any REV-003 missing seam test for a safety-relevant seam; any critical security finding.

High: REV-003 missing seam test for a non-safety seam; REV-005/REV-006 violations; any reliability gap on a critical path.

Medium: maintainability issues, missing observability on non-critical paths, conventional-commit violations.

Low: optional polish.

---

## REV-008 — Final Verdict Is One of Four (Guardrail)

**Severity:** Critical

Output exactly one verdict:
- **APPROVE** — no findings, or only low/optional findings
- **APPROVE WITH COMMENTS** — medium findings only; safe to merge, should address
- **REQUEST CHANGES** — high findings present; must fix before merge
- **BLOCKED** — any blocker finding present; human escalation required; genesis-ai[bot] does not commit

Never leave the verdict ambiguous. Never output "it depends" or "mostly good but...". One of the four, with a one-sentence rationale.
