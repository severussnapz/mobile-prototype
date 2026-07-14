# Skill: Seam Testing Discipline
**Stage:** P11 — TDD / Code Generation (Test Writer agent)
**Injection:** All phases of P11

---

## SEAM-001 — Inventory Seams Before Writing Tests (Guardrail)

**Severity:** Critical

Before writing any test, list every producer→consumer seam the feature introduces. A seam is any point where one component hands something to another — a result field crossing the HTTP boundary, an artefact written and later read back, a tool registered and later executed, a command with a controller route.

**Required first step for every P11 session:**
```
Seams introduced by this feature:
1. [ResultType.FieldX] → HTTP response body
2. [CommandY] → controller route
3. [ArtefactZ] written → read back at [point]
4. [ToolName] registered → wiring in ExecuteToolCallAsync
```

Do not write a single test until this list is complete.

---

## SEAM-002 — One Seam Test Per Seam (Guardrail)

**Severity:** Critical

For every seam on the inventory list, there must be a test that fails if the handoff is incomplete. The five types:

**Type 1 — Result → HTTP body.**
For every new field on a command/query result type: assert it appears in the serialised HTTP response body. Reflect over the result type if needed. A field present in the result but absent from the response DTO is a bug — and it is green by default.

**Type 2 — Command → route existence.**
For every new command or query: assert a controller route dispatches to it. The missing route is always green until this test exists.

**Type 3 — Artefact write → read-back (stronger form required).**
For every artefact type that will be consumed after writing: write an integration test that (a) writes the artefact, (b) resumes the consuming context, (c) asserts the content is present where the consumer expects it. "A read path exists" is not sufficient — the test must prove the loop closes.

**Type 4 — Tool registration → wiring.**
For every new tool added to `PipelineToolDefinitions`: assert `ExecuteToolCallAsync` handles it by name. "Unknown tool call" in production logs is this test missing.

**Type 5 — Pin → resolution.**
For any new version-pinning mechanism: assert the pinned version is what the consumer receives — not latest.

**Anti-rationalization:** "The connection is obvious" — it was obvious for every seam failure in the history of this codebase. Write the test.

---

## SEAM-003 — Tests Assert Behaviour, Not Implementation (Guardrail)

**Severity:** Critical

A test that mirrors the implementation is not a test — it is a transcription. It will pass after every refactor including the wrong ones.

**Forbidden patterns:**
```csharp
// ❌ Asserts that the mock was called in a specific internal sequence
// ❌ Asserts that a private field was set to a specific internal value
// ❌ Asserts on a mock return value the test itself configured, proving nothing
```

**Required pattern:** assert on the observable outcome from the user's perspective — the HTTP response body, the DB state after the operation, the content in the rebuilt prompt, the tool result returned to the agent.

Ask before writing each assertion: "Could this test pass if the feature were deleted?" If yes, rewrite it.

---

## SEAM-004 — Real Captures, Not It.IsAny (Guardrail)

**Severity:** High

When a test needs to assert on a value passed to a mock — a system prompt, a tool input, a message list — capture the actual argument and assert on it. `It.IsAny<T>()` in a mock setup asserts nothing about content.

**Required capture pattern (Moq):**
```csharp
var captured = new List<AiSystemPrompt>();
mock.Setup(s => s.StreamWithToolsAsync(
        It.IsAny<AiSystemPrompt>(), ...))
    .Callback<AiSystemPrompt, ...>((prompt, ...) => captured.Add(prompt))
    .Returns(...);

// After act:
Assert.NotEmpty(captured);
Assert.Contains(captured, p =>
    (p.StablePart + p.MutablePart).Contains("EXPECTED-MARKER"));
```

**Do not use** `Capture.In` with `SetupSequence` — they compose unreliably. Use `Callback`.

---

## SEAM-005 — Seed State Must Match Production Filter (Guardrail)

**Severity:** Critical

Before writing an integration test that seeds data and expects production code to find it: verify the production query's filter conditions and seed exactly that state — not approximate state.

**Required check:** read the repository implementation (`Where` clause) before writing the seed. If production filters `IsPublished == true`, the test seeds a published record. If production orders `OrderByDescending(Version)`, the test confirms the latest version is returned.

"It's similar data" is not sufficient. A test that seeds the wrong state goes green for the wrong reason and goes red when it should be green.

---

## SEAM-006 — Helper Return Types Must Be Concrete (Guardrail)

**Severity:** High

Test helper methods must declare concrete return types matching the element they produce. Returning `IReadOnlyList<object>` or `dynamic` from a helper that produces domain entities is a type-erasure shortcut — invisible at RED, it detonates as a phantom compile error when the production types are created.

**Forbidden:**
```csharp
❌ private static IReadOnlyList<object> CreateAllPins() { ... }
```

**Required:**
```csharp
✅ private static IReadOnlyList<ContractManifestPin> CreateAllPins() { ... }
```
