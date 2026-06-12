---
name: conventional-commits
description: >
  Prescriptive rules for writing conventional commit messages. Use this skill
  when writing or reviewing any git commit message — including feat, fix,
  refactor, perf, style, test, docs, build, ops, chore, and breaking-change
  commits. Covers format, type selection, scope usage, description style,
  body/footer content, breaking change indicators, commit signing, branch
  naming, and versioning impact.
metadata:
  version: 3.0.0
  applyTo: "*"
---

# Conventional Commits

All commit messages in this repository must follow the Conventional Commits
specification.

## Format

```
<type>[optional scope][optional !]: <description>

[optional body]

[optional footer(s)]
```

### Special Cases

| Scenario | Format |
|----------|--------|
| Initial commit | `chore: init` |
| Merge commit | `Merge branch '<name>'` (default git merge message) |
| Revert commit | `Revert "<original subject>"` (default git revert message) |

## Commit Types

| Type | When to use |
|------|-------------|
| `feat` | Adds, adjusts, or removes a feature visible in the API or UI |
| `fix` | Fixes an API or UI bug introduced by a previous `feat` commit |
| `refactor` | Rewrites or restructures code without altering API/UI behaviour |
| `perf` | A `refactor` that specifically improves performance |
| `style` | Whitespace, formatting, missing semicolons with zero behaviour change |
| `test` | Adds missing tests or corrects existing tests |
| `docs` | Documentation-only change |
| `build` | Build tooling, dependencies, or version updates |
| `ops` | Operational change: infrastructure, deployment, CI/CD, backup, monitoring, recovery |
| `chore` | Maintenance and housekeeping tasks |

Do not use non-standard types (for example `misc`).

## Scope (Optional)

Scope is optional but encouraged when the change is confined to a clear area.

```
feat(projects): add soft-delete endpoint
fix(conversations): prevent duplicate SSE events
docs(readme): add local development instructions
```

Rules:
- Use lowercase, hyphen-separated scopes
- Do not use issue identifiers as scope
- Prefer product/feature areas over generic layer names

## Breaking Changes

Breaking changes must be flagged with one of these mechanisms (both when helpful):

```
feat(api)!: remove status endpoint
feat!: change response format to JSON:API
```

Or use a footer:

```
feat: update authentication flow

BREAKING CHANGE: JWT tokens now expire after 1 hour instead of 24 hours
```

## Description Guidelines

- Use imperative present tense: "add" not "added" or "adds"
- Start with lowercase
- Do not end with a full stop
- Keep the subject concise (aim for under 72 chars)
- Be specific and meaningful

Good:

- `feat: add password reset functionality`
- `fix: prevent race condition in queue processor`
- `refactor: simplify error handling in API client`

Poor:

- `fix: bug fix` (too vague)
- `feat: Updated the thing.` (wrong tense, has full stop)
- `misc: stuff` (not a valid type, not descriptive)

## Body (Optional)

Use the body when subject alone does not explain the reason for change.
Explain what changed and why, not how.

```
fix: prevent duplicate form submissions

The submit button was not being disabled after the first click,
allowing users to accidentally submit the form multiple times.

Closes #123
```

## Footer (Optional)

Common footer tokens:

- `Closes #123` or `Fixes #123` - Links to issues
- `Refs #456` - References related issues
- `BREAKING CHANGE: description` - Breaking change details
- `Co-authored-by: Name <email>` - Co-authors

Do not place issue identifiers in scope; put them in footer.

## Versioning Impact

When determining semantic version impact:

- `BREAKING CHANGE:` footer or `!` in subject -> major
- `feat` or `fix` (without breaking change) -> minor
- all other types -> patch

## Examples

Feature:

```
feat: add email notifications on new direct messages
```

Feature with scope:

```
feat(shopping-cart): add the amazing button
```

Breaking feature:

```
feat!: remove ticket list endpoint

BREAKING CHANGE: ticket endpoints no longer support list all entities
```

Fix:

```
fix(api): fix wrong calculation of request body checksum
```

Performance:

```
perf: decrease memory footprint for unique visitor tracking
```

Build:

```
build(release): bump version to 1.0.0
```

Style:

```
style: remove empty line
```

## Commit Signing

All repositories require verified commit signatures by branch protection.

Rules:
- Always sign commits
- Do not bypass signing failures with `commit.gpgsign=false`
- If unsigned by mistake: `git commit --amend --no-edit -S` and push with `--force-with-lease`
- Verify with `git log --show-signature -1`

Configuration examples:

```bash
# SSH signing
git config user.signingkey ~/.ssh/id_ed25519_signing.pub
git config gpg.format ssh
git config commit.gpgsign true

# GPG signing
git config user.signingkey <KEY_ID>
git config commit.gpgsign true
```

## Branch Naming

Use conventional type prefix plus kebab-case description:

```
<type>/<short-description>
```

Good:

```bash
feat/add-user-authentication
fix/token-refresh-race-condition
docs/update-api-reference
test/add-integration-tests
```

Bad:

```bash
feature/add-user-authentication
my-branch
FEAT/Add-Authentication
feat/add_user_auth
```

## Rule Summary

- Type must be valid and meaningful
- Scope is optional but must be clean when used
- Description must be imperative, lowercase start, no trailing period
- Breaking changes must be clearly indicated
- Body and footer should explain context and references
- Signing and branch naming conventions are mandatory in this repository


```
feat(api): add user preferences with settings page

- Add UserPreferences entity and repository
- Create GET/PUT /api/preferences endpoints
- Add preferences section to settings UI
```

## Automated Releases

Conventional commits enable automated versioning:

- `fix:` → Patch release (1.0.0 → 1.0.1)
- `feat:` → Minor release (1.0.0 → 1.1.0)
- `feat!:` or `BREAKING CHANGE:` → Major release (1.0.0 → 2.0.0)

## Commit Signing

All EMIS-X repositories require **verified commit signatures** via branch
protection rules. Unsigned commits will be rejected at push or blocked from
merging.

### Rules

- **Always sign commits** — honour the repository's configured signing method
  (GPG or SSH). Never disable signing with `-c commit.gpgsign=false` or
  equivalent flags.
- **Diagnose, don't bypass** — if signing fails, investigate the cause (missing
  key, expired key, misconfigured `gpg.format`) rather than disabling it.
- **Amend unsigned commits** — if a commit was accidentally created without a
  signature, amend it: `git commit --amend --no-edit -S`, then force-push with
  `--force-with-lease` (never `--force`).
- **Verify before pushing** — run `git log --show-signature -1` to confirm the
  latest commit is signed.

### Common Signing Configuration

```bash
# SSH signing (preferred)
git config user.signingkey ~/.ssh/id_ed25519_signing.pub
git config gpg.format ssh
git config commit.gpgsign true

# GPG signing
git config user.signingkey <KEY_ID>
git config commit.gpgsign true
```

### Gotchas

- **`--no-verify` does not skip signing** — it bypasses Git hooks (e.g. Husky
  pre-commit), not GPG/SSH signing. Signing is controlled separately by
  `commit.gpgsign`.
- **CI environments** — signing may not be available in all CI runners. When
  creating commits in GitHub Actions, use the `actions/github-script` action or
  GitHub API which produce GitHub-verified commits automatically.
- **Force-push safety** — when amending for a missing signature, always use
  `--force-with-lease` to avoid overwriting collaborators' work.

## Branch Naming

Branch names must use a conventional commit type as their prefix, followed by `/` and a kebab-case description:

```
<type>/<short-description>
```

**Good:**

```bash
feat/add-user-authentication
fix/token-refresh-race-condition
refactor/extract-validation-logic
docs/update-api-reference
chore/upgrade-dependencies
test/add-integration-tests
```

**Bad:**

```bash
feature/add-user-authentication   # ❌ "feature" is not a valid commit type — use "feat"
my-branch                         # ❌ no type prefix
FEAT/Add-Authentication           # ❌ uppercase — use all lowercase
feat/add_user_auth                # ❌ underscores — use kebab-case
```

> ⚠️ Do NOT use `feature/` — this is a GitFlow convention. The correct prefix
> is `feat/` to align with the conventional commit type.

---

## Quick Reference

```
feat: add new feature
fix: fix a bug
docs: documentation only
style: formatting, no code change
refactor: code change, no feature/fix
perf: performance improvement
test: add/update tests
build: build system/dependencies
ci: CI configuration
chore: maintenance
revert: revert previous commit

feat!: breaking change
feat(scope): scoped change
```
