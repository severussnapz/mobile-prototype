# Skill: Testing Strategy — Shape, Layers, and Test Quality

**Apply whenever:** deciding what *kinds* of tests a feature needs, reviewing whether a suite's shape matches its risk, triaging flaky tests, or judging whether "N tests passing" actually means anything. TDD mechanics (tdd-red-green-discipline.md) govern how a test is written; this skill governs which tests should exist.

---

## Shape follows risk, not dogma

The pyramid (many unit, fewer integration, few E2E) is a default, not a law. The real rule: **each layer earns its place by catching a failure class the cheaper layer can't.**

- **Unit tests** — domain invariants and logic branches. The ContractManifest factory's six-role check belongs here: fast, precise, no infrastructure. If a behaviour can be proven at this layer, prove it here.
- **Integration tests** — the seams: HTTP boundary, DB round-trips, tool wiring, prompt assembly. Genesis's silent-seam history (DTO drop, SESSION-CLOSE write-only, missing route) is exactly the class unit tests structurally cannot catch — which is why the seam-test family (seam-testing.md) lives at this layer. WebApplicationFactory + mocked externals + real internal wiring is the house pattern.
- **Container/environment tests** — catch config-and-composition failures (DI resolution, migrations against real Postgres, Testcontainers). A suite green in-memory can still fail to *start* — the container test is what caught the missing PATCH route.
- **E2E** — few, critical-journey-only, because they're slow and blur blame. One per pipeline-critical flow (create project → generate → approve → push) beats fifty screen-level scripts.

Anti-patterns in shape: an ice-cream cone (everything E2E — slow, flaky, undiagnosable) and a false pyramid (thousands of unit tests all mocking the seams where the real bugs live — Genesis's 900-green-tests-while-fields-vanished history is the cautionary tale).

## Contract tests — the layer the pipeline makes load-bearing

Where two independently-built sides meet a shared contract (frontend/NSwag types ↔ API; TDD agent's tests ↔ code agent's implementation), test each side against the *contract*, not against each other: the API's responses validate against API-CONTRACT.yaml; the frontend builds against generated types from the same pinned version. This is what makes the two-agent architecture's "test suite as collision point" work — the contract test is the referee both sides answer to, and it's what turns the frozen contract from documentation into enforcement.

## Test quality: a green suite can prove nothing

- **A test that can't fail is a liar.** For any suspicious test: could it pass if the feature were deleted? Assertions on mocks the test itself configured, `NotNull` checks on things that can't be null, and captured-but-never-asserted values are theatre. The mutation-testing *mindset* — "what code change would this test catch?" — applied during review catches most of what mutation tooling would, for free.
- **Coverage is a gap-finder, not a target.** Low coverage on a critical path is actionable; a mandated percentage breeds assertion-free tests written to move a number. Never gate on coverage alone.
- **One behaviour per test, named as behaviour** (`Method_WhenCondition_ExpectedResult`): the failure message should diagnose without opening the file.
- **Tests are production code**: duplication, magic values, and unreadable arrange blocks rot suites into untouchability — at which point people stop trusting and start deleting.

## Flaky tests: quarantine, diagnose, fix — never retry-and-forget

A flaky test destroys the suite's core asset: the meaning of green. Discipline:

1. **Never delete on flake, never blind-retry as policy.** Auto-retry hides real race conditions — the flake is often a genuine concurrency or ordering bug in *production code* announcing itself.
2. **Quarantine visibly** (skip with a tracked reason and owner) so the suite stays trustworthy while the flake is diagnosed — an untracked skip is a silent hole.
3. **Diagnose the class**: shared state between tests, time dependence (use the injected TimeProvider — never `DateTime.UtcNow` in code *or* tests), ordering assumptions, real-infrastructure timing (Testcontainers startup), async races. Each class has a standard fix; "ran it again and it passed" is not one.
4. **Environment-conditional skips are declared, not accidental** (`RequiresDocker` skip-when-unavailable is legitimate *because* it's explicit and counted — 4 skipped is a known number, not a mystery).

## The regression contract

Every escaped defect gets a test that would have caught it *before* the fix (red on the broken code, green after) — and per the standing meta-rule, a defect representing a new *class* gets a new test-type in the family, not just an instance test. The suite is the accumulated immune memory of everything that ever got through; that's why its integrity (no weakened assertions, no untracked skips, no lying greens) is guarded as hard as production code.
