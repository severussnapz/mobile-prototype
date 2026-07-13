# Genesis SDLC Skills Bundle

Seven senior-expert SDLC skills for the Genesis AI programme. Written to be added to the project KB (`src/Genesis.AI.Infrastructure/KnowledgeBase/` and/or the Claude project knowledge) so any model working in this project — Opus, Sonnet, or a pipeline agent — operates at the standard the programme requires.

Each file opens with an **"Apply whenever"** trigger so a model scanning the KB knows when the skill is in force without reading the whole body.

## The seven skills

| File | One-line purpose |
|---|---|
| `agent-supervision.md` | Verify-don't-trust: the post-implementation audit, the five documented agent cheat patterns, confession-language tells, outcomes-not-prohibitions prompt phrasing. |
| `tdd-red-green-discipline.md` | The two-prompt RED/GREEN split hardened for agents: verifying the RED is honest, count prediction, real captures, prompt-construction rules. |
| `seam-testing.md` | The silent-seam failure class (produced at one end, dropped at the other, all green) and the five seam-test types that kill it. |
| `design-integrity.md` | Proven vs assumed: safety-by-arrangement flagging, no unearned numbers, elevating load-bearing decisions, verify-the-pattern-exists before reusing it. |
| `verify-before-claim.md` | Grep-first: never state signatures, filters, defaults, migration numbers, or pattern-existence from memory; let the build diagnose. |
| `branch-commit-hygiene.md` | Clean-baseline audits, exp/PR branch discipline, staged logical commits, duplicate-commit handling, agent-on-wrong-branch prevention. |
| `regulated-engineering.md` | The judgement layer for the NHS/clinical context: severity-over-probability, strict-from-day-one gates, human-in-the-loop as designed-in, data absolutes, audit-trail thinking. |

## How they relate to existing KB documents

These **complement, not replace**:
- `ponytail.md` — minimal-code ladder (unchanged; `tdd-red-green-discipline.md` and `design-integrity.md` reference its spirit)
- `coding-standards.md` — base TDD steps and conventions (extended by `tdd-red-green-discipline.md` for the agent era)
- `debug-learnings.md` — accumulated fixes (extended by `verify-before-claim.md`'s diagnose-with-the-build rule)
- `.github/copilot-instructions.md` rules 1–5 — the agent-facing prohibitions (`agent-supervision.md` is the human/reviewer-facing counterpart: how to catch what the prohibitions miss)

## Provenance

Every rule in this bundle was earned, not theorised: the five cheat patterns were caught live (null-object bypass, CA1859 suppression, build-props global-using, plus test-weakening and type-erasure classes); the seam-test family came from three real production-shaped gaps (DTO mapping, SESSION-CLOSE write-only artefact, missing PATCH route); the design-integrity rules came from three corrections applied to a live design document; the regulated-judgement rules from the contract-layer and tagging-governance design sessions.

## Suggested adoption order

1. Commit all seven to the KB in one commit.
2. Add the two most safety-critical (`agent-supervision.md`, `regulated-engineering.md`) to the Claude project knowledge if not already indexed.
3. Reference `seam-testing.md` from the Review Agent prompts (GENESIS-008 is its enforcement arm).
4. Revisit after the next implementation phase: any newly caught cheat or seam type gets added to its family file — never just fixed as an instance.
