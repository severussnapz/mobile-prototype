---
name: commit-skills
description: >
  Prescriptive rules for writing conventional commit messages in the Genesis AI Requirements API. Use this skill when writing or reviewing any git commit message — including feat, fix, refactor, perf, style, test, docs, build, ops, chore, and breaking-change commits. Covers format, type selection, scope usage, description style, body/footer content, breaking change indicators, and versioning impact. Rules are prefixed CMT and must be satisfied by all commit messages.
metadata:
  version: 1.0.0
  applyTo: ["genesis-ai-requirements-api"]
---

# Commit Skills — Conventional Commit Messages

All commit messages in this repository must follow the Conventional Commits specification.

---

## Format

```
<type>[optional scope][optional !]: <description>
<empty line>
[optional body]
<empty line>
[optional footer(s)]
```

### Special cases

| Scenario | Format |
|----------|--------|
| Initial commit | `chore: init` |
| Merge commit | `Merge branch '<name>'` (default git message — do not alter) |
| Revert commit | `Revert "<original subject>"` (default git revert message — do not alter) |

---

## CMT-001 — Type Selection (Guardrail)

**Severity:** Critical

Every commit subject must begin with one of the approved types:

| Type | When to use |
|------|-------------|
| `feat` | Adds, adjusts, or removes a feature visible in the API or UI |
| `fix` | Fixes an API or UI bug introduced by a previous `feat` commit |
| `refactor` | Rewrites or restructures code without altering API/UI behaviour |
| `perf` | A `refactor` that specifically improves performance |
| `style` | Whitespace, formatting, missing semicolons — zero behaviour change |
| `test` | Adds missing tests or corrects existing ones |
| `docs` | Affects documentation exclusively |
| `build` | Affects build tooling, dependencies, or project version |
| `ops` | Affects infrastructure, deployment, CI/CD, backups, monitoring, recovery |
| `chore` | Maintenance: initial commit, `.gitignore`, etc. |

**Compliant:**
```
feat(conversations): add SSE streaming for AI responses
fix(auth): return 401 on expired refresh token
ops: add health-check endpoint to docker-compose
```

**Non-compliant:**
```
misc: stuff                  ← invalid type
updated the thing            ← no type at all
feat: Updated the thing.     ← wrong tense, trailing period
```

---

## CMT-002 — Scope (Steer)

**Severity:** Advisory

Scope is **optional** but encouraged when the change is confined to a specific feature area.

- Use lowercase, hyphen-separated words: `auth`, `conversations`, `artefacts`, `pipeline-stages`, `prompts`
- Do **not** use issue identifiers as scope: ~~`feat(GENAI-123): ...`~~
- Do **not** use layer names alone: ~~`feat(controller): ...`~~

**Compliant:**
```
feat(projects): add soft-delete endpoint
fix(conversations): prevent duplicate SSE events
docs(readme): add local development instructions
```

**Evidence required:** When scope is omitted for a large change, the commit body should explain which areas are affected.

---

## CMT-003 — Description Style (Guardrail)

**Severity:** Critical

The description (subject line after the type/scope) must:

1. Use the **imperative, present tense**: "add", "fix", "remove" — not "added", "adds", "adding"
2. Start with a **lowercase** letter
3. **Not** end with a period (`.`)
4. Be **concise** — aim for under 72 characters total subject length

Mental model: *"This commit will..."* or *"This commit should..."*

**Compliant:**
```
feat: add email notifications on new direct messages
fix(api): fix wrong calculation of request body checksum
perf: decrease memory footprint for unique visitor tracking
```

**Non-compliant:**
```
feat: Added email notifications    ← past tense
feat: Add email notifications.     ← trailing period
feat: Add Email Notifications      ← capitalised first letter
fix: fixed it                      ← past tense, vague
```

---

## CMT-004 — Breaking Change Indicator (Guardrail)

**Severity:** Critical

A commit that introduces a **breaking change** must be flagged. Two mechanisms — use at least one; use both if the subject alone is ambiguous:

**Mechanism 1 — `!` in the subject line:**
```
feat(api)!: remove status endpoint
feat!: change response format to JSON:API
```

**Mechanism 2 — `BREAKING CHANGE:` footer:**
```
feat: update authentication flow

BREAKING CHANGE: JWT tokens now expire after 1 hour instead of 24 hours.
```

For a **multi-line** breaking change description, add two newlines after `BREAKING CHANGE:`:
```
feat!: remove ticket list endpoint

BREAKING CHANGE:
Ticket endpoints no longer support listing all entities.
Clients must use paginated GET /api/v1/tickets?page[size]=50 instead.
```

---

## CMT-005 — Body Content (Steer)

**Severity:** Advisory

The body is optional but **should** be included when the subject alone doesn't convey the *why*:

- Use imperative, present tense
- Explain **what** changed and **why** — not *how* (the diff shows how)
- Separate from the subject with one blank line

**Compliant:**
```
fix: add missing parameter to service call

The error occurred because the userId parameter was silently dropped
when constructing the downstream Bedrock request. Added null guard
and forwarded the value correctly.
```

---

## CMT-006 — Footer Content (Steer)

**Severity:** Advisory

The footer is optional except when introducing breaking changes (see CMT-004).

Approved footer tokens:

| Token | Purpose |
|-------|---------|
| `Closes #NNN` / `Fixes #NNN` | Link to GitHub issue — closes on merge |
| `Refs #NNN` | Reference a related issue without closing it |
| `BREAKING CHANGE: ...` | Mandatory for breaking changes (see CMT-004) |
| `Co-authored-by: Name <email>` | Credit co-authors |

Do **not** include issue identifiers in the scope — put them in the footer.

---

## CMT-007 — Versioning Impact (Steer)

**Severity:** Advisory

When deciding the next version number, follow this rule:

| Contains | Version bump |
|----------|-------------|
| Any commit with `BREAKING CHANGE:` or `!` | **Major** (`x.0.0`) |
| Any `feat` or `fix` commit (no breaking change) | **Minor** (`0.x.0`) |
| Everything else (`refactor`, `perf`, `style`, `test`, `docs`, `build`, `ops`, `chore`) | **Patch** (`0.0.x`) |

**Evidence required:** When raising a PR that bumps the version, state which commit(s) drove the version decision.

---

## Quick Reference — Examples

```bash
# Feature
feat: add email notifications on new direct messages
feat(pipeline-stages): add bulk stage completion endpoint

# Bug fix
fix(shopping-cart): prevent ordering an empty cart
fix(api): correct wrong checksum calculation in request body

# Breaking feature
feat!: remove ticket list endpoint

BREAKING CHANGE: ticket endpoints no longer support list all entities.

# Performance
perf: decrease memory footprint for unique visitor tracking using HyperLogLog

# Build / deps
build: update dependencies
build(release): bump version to 1.0.0

# Refactor
refactor: implement fibonacci calculation as recursion

# Style
style: remove empty line

# Initial commit
chore: init
```
