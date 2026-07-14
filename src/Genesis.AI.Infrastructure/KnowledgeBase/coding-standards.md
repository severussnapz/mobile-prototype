# Genesis AI — Coding Standards & Conventions

## Non-Negotiable Rules

### Tests First — Always
1. Write the test
2. Run it — confirm RED
3. Write the minimum code to make it pass
4. Run it — confirm GREEN
5. Build full solution
6. Run full test suite
7. Rebuild container with `--no-cache`
8. Commit

No exceptions. No "I'll add tests later". Red before green.

### Commit Format
```
fix(plan4): description of what was fixed — impact on behaviour
test(plan4): TestMethodName red-first

feat(plan3c): description of new capability
test(plan3c): TestMethodName_WhenCondition_ExpectedResult red-first
```

### Test Method Naming
```csharp
MethodName_WhenCondition_ExpectedResult
// Examples:
ApplyBulkAttributes_WhenTextSnippetMissing_RejectsWithValidationFailure
FormatListElementsBulkResult_IncludesWorkedExample
ApplyToScope_WhenScopeNotFound_ReturnsError
```

---

## Container Discipline

Always verify deployed code is actually running:
```bash
# Check container timestamp (UTC) vs commit timestamp (+0100)
docker inspect genesis-ai-requirements-api-api-1 --format '{{.Created}}'
git log --oneline -1 --format="%ci"

# Container 21:05 UTC = 22:05 BST
# Commit 21:07 BST = 20:07 UTC
# Container IS newer — correct

# Always rebuild with --no-cache after source changes
docker compose build --no-cache api && docker compose up -d api
```

**Never trust a container that predates the last commit.**

---

## Coding Agent Instructions Template

Send to coding agent in this exact format:

```
TESTS FIRST — write these red before any code changes:

TEST 1: [MethodName_WhenCondition_ExpectedBehaviour]
  - Populate: [what state to set up]
  - Call: [what method/endpoint to invoke]
  - Assert: [what to verify]
  → RED with current code because [specific reason]

Run both — confirm RED. Then implement:

CHANGE 1 — [filename]:
  [Specific change description]

CHANGE 2 — [filename]:
  [Specific change description]

Run tests — GREEN.
Build: dotnet build Genesis.AI.sln → 0 errors
Full suite: dotnet test tests/Genesis.AI.Tests/Genesis.AI.Tests.csproj
Rebuild: docker compose build --no-cache api && docker compose up -d api
```

**Never ask the coding agent to hypothesise and implement in the same prompt. Hypothesis first, then a separate implementation prompt.**

---

## Architecture Principles

### LLM vs API Responsibilities
```
LLM does:                    API does:
───────────────────          ────────────────────────────
Understand intent            Find elements
Describe the operation       Generate values
Choose strategy              Apply mutations
Name the scope               Verify N of N
Select selector              Report result
```

### Fail-Closed
Every operation either succeeds completely or fails completely. No partial writes. No silent failures. If N of N mutations didn't apply — it's a failure, not a success.

### Immutable Outputs
Every pipeline output is versioned in S3. Never overwrite. Always create a new version. Rollback is always possible.

### Deterministic-First
LLM generates intent. Deterministic code executes. Never the reverse.

---

## Solution Structure

```
genesis-ai-requirements-api/
├── src/
│   ├── Genesis.AI.Api/
│   │   └── Features/Conversations/
│   │       └── ConversationStreamController.cs  ← main controller
│   ├── Genesis.AI.Domain/
│   │   └── Interfaces/                           ← contracts
│   ├── Genesis.AI.Infrastructure/
│   │   └── Services/
│   │       ├── ArtefactToolBuilder.cs            ← tool schemas
│   │       ├── PipelineToolDefinitions.cs        ← tool name constants
│   │       ├── PrototypeDomMutationService.cs    ← DOM mutations
│   │       └── PrototypeDomSearchService.cs      ← DOM search
│   └── Genesis.AI.Core/
└── tests/
    ├── Genesis.AI.Tests/                         ← unit tests (601+)
    └── Genesis.AI.IntegrationTests/              ← needs running env (96+)
```

## Active Branches
- `genesis-ai-requirements-api`: `plan4-dom-mutation`
- `genesis-ai-requirements-app`: `plan4-dom-mutation-app`

---

## Common Pitfalls

1. **Merge conflicts after revert** — always `git revert --abort` if conflicts appear, then fix in place
2. **Stale container** — always check timestamps, always `--no-cache`
3. **96 integration test failures** — these need a running environment, not a code issue
4. **Model claiming Done** — never trust model narrative, always check S3 for written fragment
5. **text_snippet mandatory in schema** — model stops calling tool entirely, keep optional
6. **apply_bulk_attributes offset** — root cause is model semantic ordering, not solvable by prompting
