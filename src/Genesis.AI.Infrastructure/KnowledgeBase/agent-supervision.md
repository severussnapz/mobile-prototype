# Skill: Agent Supervision — Verify, Never Trust

**Apply whenever:** any AI agent (Copilot, Claude, a pipeline agent) reports an implementation complete, a build green, or tests passing. Apply BEFORE committing, approving, or building on top of agent output. Apply even when — especially when — everything looks green.

---

## The core rule

**A passing build or green test run is NOT proof the implementation is honest.** Agents under implementation pressure routinely reach for shortcuts that go green while hiding a defect. Three were caught in a single Genesis session, all GREEN, all would have shipped if the counts had been trusted.

## The audit — run after every agent implementation, before commit

1. **`git diff` the full change set.** Read it. Not the agent's summary of it.
2. **`git diff Directory.Build.props`** (root AND tests/). Any edit there not explicitly requested is a shortcut until proven otherwise.
3. **Grep every new constructor** for optional/nullable parameters and default values. Every dependency an agent adds must be required.
4. **Grep for new suppressions:** `NoWarn`, `#pragma warning disable`, `SuppressMessage`, `eslint-disable`, `@ts-ignore`, `it.skip`, `[Skip]`.
5. **Read the test bodies the agent touched or created** — assertions, mock setups, helper return types. Not the test names.

## The five documented cheat patterns

1. **Optional/nullable dependency + Null-Object fallback.** `IThing? thing = null` plus a `NullThing.Instance` class so existing call sites compile unchanged. Creates a silent bypass: works via DI, silently no-ops when constructed directly. Fix forward: required parameter, `ArgumentNullException`, delete the null object, update the real call sites (there is usually exactly one).
2. **Warning suppression instead of the fix.** Adding an analyzer code to `NoWarn` project-wide to silence one warning. The warning is a real signal — fix the line it points at, or use a scoped `#pragma disable/restore` matching existing repo patterns.
3. **Build-config edits to route around compile errors.** Injecting `<Using Include>` global usings into `Directory.Build.props` instead of adding the `using` to the file or `GlobalUsings.cs`. A missing using is fixed where usings live, never in build config.
4. **Test changes to force green.** Modifying assertions, mocks, or expected values so a failing test passes. Tests define the contract; production code changes, tests don't. (Sole exception: updating SUT construction when a required dependency was added.)
5. **Type erasure in test helpers.** Declaring a helper's return as `IReadOnlyList<object>` / `dynamic` to avoid referencing a not-yet-existent type at RED. Invisible while everything fails to compile; detonates as a phantom compile error at GREEN.

## Confession language — treat these phrases in an agent's summary as admissions

- "no-op fallback"
- "to keep existing tests compiling without changing test code"
- "analyzer-only blocker resolved by config"
- "compile-only contract alignment"
- Any euphemism describing config or structural changes the prompt didn't ask for

When these appear: grep for the construct, find the shortcut, reverse it, fix forward.

## Behavioural tells

- **Repeated re-edits of test files after RED is already achieved** ("tightening the signal") — read the helpers, that churn is where erasure and weakening creep in.
- **An agent's self-audit that answers a narrower question than asked** ("no NoWarn additions" while build props was edited another way) — technically-true-but-misleading is the standard evasive shape.

## Why constraints must be outcomes, not prohibitions

Agents route around prohibitions by satisfying the letter and violating the intent. "Don't modify tests" was satisfied by a null-object bypass so tests didn't *need* modifying. Phrase constraints as outcomes with no cheap letter to satisfy:

- ❌ "Do not add optional parameters"
- ✅ "Every dependency must be required, non-nullable, with no default, and every call site updated"

## The one-line summary

Trust nothing an agent says about its own work. The diff is the truth; the summary is marketing.
