# Skill: TDD Red/Green Discipline (Agent-Hardened)

**Apply whenever:** writing or directing any implementation work — new features, fixes, refactors — in the Genesis codebase or any regulated codebase. Apply especially when an agent (Copilot, Claude) is doing the implementation. This extends coding-standards.md with the agent-era hardening that plain TDD lacks.

---

## The two-prompt hard rule

Implementation via an agent is ALWAYS two separate prompts:

- **Prompt 1 — tests only.** The tests reference production types/methods that do not yet exist. Expected outcome: compile failure (CS0246 missing type) or failing assertions. Confirm the failure is the *right* failure — a missing type, not a broken test.
- **Prompt 2 — implementation only.** No test file modifications permitted. Expected outcome: GREEN with the exact predicted count increase.

Never one prompt that writes tests and implementation together — the agent will write tests that mirror its implementation, which is transcription, not testing.

## RED is not enough — verify the RED is honest

A RED state can hide defects that only detonate at GREEN. Before Prompt 2:

1. **Read the test bodies** (not names). Confirm assertions are genuine: `Assert.Throws` around the actual violating call, exact-equality against fixed values (a `FakeTimeProvider` with a fixed timestamp, not "is not default"), full mapping checks (all N items, not just `Count == N`).
2. **Read the helpers.** Return types must be concrete — `IReadOnlyList<TheRealType>`, never `object`/`dynamic`. Type-erased helpers compile at RED (everything fails anyway) and break at GREEN as phantom errors.
3. **Confirm the failure mode.** The build must fail on the missing production type specifically. Any other failure means the tests are wrong, not the types.
4. **Confirm seeded state matches what production will query.** If production filters `IsPublished == true`, the test must seed published data — check the seed path's actual behaviour in code, don't assume.

## Verify counts, always

Before Prompt 2, know the current counts. After Prompt 2, the increase must equal the number of tests written — no more (agent added unrequested tests), no fewer (agent skipped or weakened one), and zero regressions. State expected counts in the prompt so the agent must report against a prediction.

## Test design rules

- Tests derive from **user-facing behaviour and acceptance criteria**, never from the implementation. Ask "what should be possible after this change?" before writing an assertion.
- A test asserting mock-interaction sequences that mirror the code's internal call order is a transcription — it breaks on refactor and proves nothing.
- Every capture must be real: to assert on an argument (e.g. a system prompt passed to a mocked service), use a `Callback` that stores it, then assert on the stored value. `It.IsAny<T>()` in a mock setup asserts nothing about content.
- Moq note: `Capture.In` composes badly with `SetupSequence` — mandate `Callback` capture explicitly when both are needed, or the agent will pick the fragile one.

## Prompt construction rules

- Reference **real, verified signatures** — grep the interface/class before writing the prompt. A guessed method name causes the agent to hallucinate around it.
- Name the **exact files to read first**, with line ranges for large files.
- State expected failure/success outcomes and required verbatim reporting ("report the compile errors verbatim", "report exact pass/fail counts for both suites").
- Include the anti-shortcut constraints inline even though they live in the instructions file — belt and braces; agents read instructions selectively under pressure.

## After GREEN — the supervision gate

GREEN triggers the agent-supervision audit (see agent-supervision.md), never a direct commit. Diff read, build-props check, constructor grep, suppression grep — then commit tests and implementation together as one logical unit (committing separately leaves a RED commit in history).
