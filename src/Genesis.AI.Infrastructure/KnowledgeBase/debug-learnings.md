Genesis AI — Debug Learnings & Technical Decisions
Purpose
This document captures hard-won technical decisions, root causes discovered, and principles established during Genesis AI development. It exists so that no future session repeats an investigation already completed.

CRITICAL: LLM Precision Limitation
Discovery: LLMs cannot reliably perform precision DOM editing or maintain correct ordered lists for bulk operations.
Root cause: Models generate values in semantic/functional order (grouped by intent) not DOM order. When assigned to positional refs, values go to the wrong elements. This is a fundamental LLM limitation, not a prompting problem.
Evidence: apply_bulk_attributes diagnostic log showed:
ref=3 value="Toggle annotations" mapSnippet=◀ Prev  ← WRONG
ref=5 value="Delete tag"         mapSnippet=Zoom +   ← WRONG
ref=9 value="Previous page"      mapSnippet=🖨️ Print ← WRONG
What was tried and failed:

text_snippet mandatory in schema → model stopped calling tool entirely
Ordered values array → model still reorders semantically
Worked example in output → model ignored it
Bidirectional contains matching → too permissive, wrong matches
Ref accumulation (not resetting map) → helped but didn't solve root cause

Correct architecture:
LLM: translates intent → structured operation (one tool call)
     apply_to_scope({ scope, selector, operation, strategy })
API: finds elements → generates values → applies → verifies N of N
Model never sees node_ids, refs, or element lists for bulk operations.

apply_to_scope — The Right Pattern for Bulk Edits
What it replaces: list_elements + apply_bulk_attributes for any bulk operation
Tool contract:
json{
  "scope": "screen-gallery-file",
  "selector": "button",
  "operation": "set_attribute|add_class|remove_class|set_text|remove_attribute",
  "attribute": "aria-label",
  "strategy": "derive_from_text_content|literal|generate_from_context",
  "value": "optional for literal strategy"
}
Strategies:

literal — same value to all elements, deterministic, no LLM call
derive_from_text_content — API cleans TextSnippet (strip emoji, arrows, duplicates)
generate_from_context — ONE focused LLM call returns JSON [{text_snippet, value}], API matches and applies

Status: COMPLETED — Plan 3c.

DOM Migration (Plan 4) — Root Cause History
Bug 1: Queue reset inside while loop
pendingListElementsQueue declared inside while(turnsRemaining>0) — reset every turn.
Fix: Move declaration outside loop.
Bug 2: listElementsRefMap reset on every list_elements call
Refs renumbered from [1] on each call. Model used refs from snapshot N but map contained snapshot N+3.
Fix: Accumulate refs — AppendListElementRefs adds to existing map, never clears.
Bug 3: text_snippet verification rejecting all mutations
StringComparison.Ordinal exact match. Model passes cleaned aria-label not original text.
Fix: Bidirectional contains, OrdinalIgnoreCase.
Bug 4: Merge conflict markers in source
Git revert left <<<<<<< HEAD markers in ConversationStreamController.cs. Build appeared to pass (cached container). Mutations silently failed.
Fix: Always run dotnet build locally before docker compose build. Check for conflict markers after any revert.
Bug 5: Docker build caching
Container built before commit — running stale code. Hours lost debugging "deployed" code that wasn't.
Fix: Always use --no-cache after code changes. Verify container creation timestamp vs commit timestamp (UTC).
Bug 6: css: prefix on fallback node_ids
Model hallucinated hex hashes as node_ids.
Fix: css: prefix on all fallback CSS path node_ids — model can't confuse with hex hashes.
Bug 7: Digit-start guard on CSS id selectors
DomException thrown on hex node_ids starting with digit in CSS selectors.
Fix: Prepend underscore when CSS id selector starts with digit.
Bug 8: JSON naming convention mismatch between C# record and AI response
Discovery: GenerateFromContextStrategy returned empty values for all elements despite the AI returning correct JSON.
Root cause: C# record GeneratedScopeValue used Pascal case property names (TextSnippet, Value). The AI response used snake_case (text_snippet, value). PropertyNameCaseInsensitive = true handles case but not naming convention differences. Snake_case text_snippet never matched Pascal case TextSnippet.
Fix: Add PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower to the cached JsonSerializerOptions.
Rule established: When deserialising AI-generated JSON responses, always specify both PropertyNameCaseInsensitive = true AND PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower. AI models reliably produce snake_case JSON regardless of prompt instructions. Verify the contract in tests before implementing — the test would have caught this at the mock setup stage if the mock had been structured to verify the deserialized output rather than just the raw string match.
How to avoid: When writing a strategy that parses AI JSON output, write a unit test that verifies the full round-trip: mock AI returns JSON string → strategy deserialises → correct values returned. This catches naming mismatches before they reach integration.
Bug 9: Interface contract not verified before implementation
Root cause: GenerateFromContextStrategy tests used IAiService.GenerateResponseAsync with string parameters, but the actual interface takes AiSystemPrompt and IReadOnlyList<AiMessage>. The test was written from memory, not from the interface definition.
Fix: Always read the interface file before writing tests or implementations that depend on it.
Rule: Before writing any test that mocks an interface, run cat src/.../IInterfaceName.cs first. Never write mock setup from memory. The compile error is caught early but the time cost of the wrong approach is avoidable.
Bug 10: Test mirrored implementation not behaviour — ChangeStatus.Undone
Discovery: After undo, the change was stuck in Undone status with no way to re-approve it.
Root cause: Test was written as Assert.Equal(ChangeStatus.Undone, change.Status) to match the implementation, not the acceptance criteria. The AC was "human can undo and re-approve with corrections" — which requires Status = Pending after undo. The audit trail fields (UndoneBy, UndoneAt, UndoRationale) correctly record the undo, but the workflow status must return to Pending so the change is editable again.
Fix: Set Status = ChangeStatus.Pending in RequirementChange.Undo(). Update tests to assert Pending.
Rule established: Before writing any assertion on a state machine, ask "what should the user be able to do after this operation?" That answer defines the correct assertion. A test that mirrors the implementation is not a test — it's a transcription.
Bug 11: EF entity configuration not registered in OnModelCreating
Discovery: relation "RequirementChanges" does not exist at runtime despite the migration creating requirement_changes and the entity configuration mapping it correctly.
Root cause: RequirementChangeEntityTypeConfiguration was never registered via modelBuilder.ApplyConfiguration(...) in GenesisAiDbContext.OnModelCreating. EF Core ignored the configuration and fell back to its default convention — PascalCase table name RequirementChanges.
Fix: Add modelBuilder.ApplyConfiguration(new RequirementChangeEntityTypeConfiguration()); to OnModelCreating.
Rule established: Every new EF entity configuration must be explicitly registered in OnModelCreating. Adding a DbSet<T> property is not sufficient. Check GenesisAiDbContext before writing any EF configuration — and after implementation, verify the SQL EF generates uses the expected table name.
Bug 12: Tool registered in PipelineToolDefinitions but not wired in controller
Discovery: Agent called propose_requirement_change successfully (returned CHANGE_PROPOSED to the agent) but nothing was saved to the database. API logs showed Unknown tool call: propose_requirement_change.
Root cause: PipelineToolDefinitions.ProposeRequirementChange constant was defined and included in tool definitions sent to the LLM, but ConversationStreamController.ExecuteToolCallAsync had no case for it. The switch fell through to default, which logged the warning and returned "Unknown tool".
Fix: Add case PipelineToolDefinitions.ProposeRequirementChange: to ExecuteToolCallAsync. Inject ProposeRequirementChangeCommandHandler into the controller constructor.
Rule established: Every new tool added to PipelineToolDefinitions must have a corresponding case in ExecuteToolCallAsync. A ToolCallWiringTests enforcement test now verifies this automatically — it reads all public const strings from PipelineToolDefinitions and asserts each one appears as a handled case in the controller source. If this test goes red, the wiring is missing.
Bug 13: Agent tool failures debugged with code instead of HTML inspection
Discovery: Agent reported tooltips applied successfully but nothing was written to S3. Three hours spent adding code layers (debug logs, pipe validation, DOM redirect, scope resolution changes, response format changes) before discovering the root cause.
Root cause: The CSS selector .smart-view-item doesn't exist. The real class is .sv-item. This was discoverable in 30 seconds with grep -n "sv-item" fragment.html. Instead, the assumption was made that the tool was broken, not the selector.
Secondary root cause: search_in_artefact on prototype/index.html returns node_ids with prototype/index.html as the fragment path. Mutations need fragment paths like prototype/fragments/_shell.html. The agent was getting back correct search results but passing the wrong path to the mutation service.
Fix: Before any code change when a tool returns no match: grep the actual fragment HTML to verify the selector exists. One command, 30 seconds.
Rule established:

When apply_to_scope returns "no elements matched" — grep the fragment HTML for the selector before touching any code.
grep -n "class\|id" prototype/fragments/screen-01-legacy.html is always step one.
Never treat agent tool failure as a code bug until the HTML is verified.
Agent behaviour (wrong scope, wrong selector, hallucinated success) is NOT a code problem — it is a prompting/skill problem or a wrong assumption about what's in the HTML.
The correct tool for bulk attribute edits on .sv-item elements in screen-01-legacy.html is: apply_to_scope({ scope: "screen-01-legacy", selector: ".sv-item", operation: "set_attribute", attribute: "title", strategy: "derive_from_text_content" })


apply_bulk_attributes — Current State
What works:

Numeric refs replacing node_ids — model never copies hex hashes
Ref accumulation across list_elements calls
Fail-closed batch rejection — partial success = failure
text_snippet bidirectional contains when provided

What doesn't work:

Model generates values in semantic not DOM order
Model does not reliably include text_snippet despite schema + prompt

Decision: Deprecate apply_bulk_attributes for bulk operations once apply_to_scope is implemented. Keep for single targeted edits only.

Architectural Decisions (ADRs)
ADR-001: AngleSharp over Graph-based DOM
Decision: Replace graph JSON (1,028 nodes) with AngleSharp DOM parsing.
Rationale: Graph had no parent/sibling context, every edit rebuilt entire graph, no CSS selector support, brittle string replacement.
Result: Direct HTML parsing, CSS selectors, structure-aware, batch mutations in one parse cycle.
ADR-002: Numeric refs over node_ids for list_elements
Decision: list_elements returns [1], [2], [3] not hex node_ids.
Rationale: Model cannot reliably copy hex hashes without hallucination.
Result: Refs resolve server-side via listElementsRefMap.
ADR-003: apply_to_scope as the correct bulk edit pattern
Decision: Single tool call where API handles element discovery, value generation, mutation and verification.
Rationale: LLM cannot maintain DOM order for bulk value assignment. Separating intent (LLM) from execution (API) is the correct architecture.
Status: COMPLETED — Plan 3c.
ADR-004: Fail-closed batch mutations
Decision: Any validation failure in apply_bulk_attributes rejects the entire batch.
Rationale: Partial writes produce wrong data silently. All or nothing is safer.
Result: IsSuccessfulBulkApplyResult validates full "Applied N of N" format only.
ADR-005: Output Template Contracts before Code Swarm
Decision: Plan 3d (template contracts) must complete before Plan 6 (swarm).
Rationale: Swarm needs to know what "done" means per pipeline. Without schema, completion criteria are undefined.

Test Discipline

Always RED first — run test, confirm failure before writing code
Never commit with failing tests
Full suite must pass before container rebuild
Integration tests (96) require running environment — not a code failure
Container rebuild: always --no-cache after source changes
Commit format: fix(plan4): description — impact\ntest(plan4): TestName red-first
Write tests from behaviour, not implementation. Before writing any assertion ask: "what should the user be able to do after this?" That answer defines the correct assertion. Tests that mirror the implementation are transcriptions, not tests.
For state machine aggregates: assert the resulting state enables the expected next action — not just that the state changed. Example: after undo, assert Status = Pending (can re-approve) not Status = Undone (dead end).
For tool handlers: always write a wiring test that verifies the controller handles the tool — not just that the handler works in isolation. The ToolCallWiringTests enforcement test in the API repo covers this automatically for all PipelineToolDefinitions constants.
For EF entity configurations: verify the SQL EF generates uses the correct table name. A DbSet<T> property is not sufficient — the configuration must be registered in OnModelCreating.


Coding Agent Instructions (Standard)
Always send to coding agent in this format:
TESTS FIRST:
TEST: [TestMethodName]
  - Setup: [what to populate]
  - Action: [what to call]
  - Assert: [what to verify]
  → RED with current code because [reason]

Run test — RED. Then implement:
[specific changes with file references]

Build, full suite, rebuild container --no-cache.
Never let coding agent implement without tests first.
Never accept "environment fixture issue" as explanation for test failures without verifying.
Always check container creation timestamp vs commit timestamp (UTC) to confirm code is actually deployed.

Common Diagnostic Commands
bash# Check container is running latest code
docker inspect genesis-ai-requirements-api-api-1 --format '{{.Created}}'
git log --oneline -1 --format="%ci"
# Container time is UTC, git time shows +0100 offset

# ALWAYS DO THIS FIRST when apply_to_scope returns no match
grep -n "class\|id" prototype/fragments/screen-01-legacy.html | head -30
grep -n "class\|id" prototype/fragments/_shell.html | head -30
# Confirm the selector and fragment before touching any code

# Check what mutations are being applied
docker compose logs api 2>&1 | grep "apply_bulk_attributes mutation" | head -20

# Check S3 for written fragments
docker compose exec -T localstack awslocal s3 ls \
  "s3://genesis-ai-artefacts/projects/d0cf7a10-.../artefacts/prototype/fragments/screen-01-legacy.html/"

# Full log excluding SQL noise
docker compose logs api 2>&1 | grep -v "SELECT\|INSERT\|UPDATE\|Executed\|DbCommand\|Microsoft\|EntityFramework" | tail -60

# Check for merge conflicts in source
grep -n "<<<<<<\|=======\|>>>>>>>" src/Genesis.AI.Api/Features/Conversations/ConversationStreamController.cs

# Verify new code actually in DLL (doesn't work reliably for .NET IL)
# Instead: check build output timestamp vs commit timestamp

---

## July 2026 Session Learnings

### Bug 14: Conversation history cap silently breaking agent context
**Discovery:** Agent asked user to repeat all captured requirements mid-session after 25 turns.
**Root cause:** maxHistoryMessages = 4 sent only last 4 messages to Bedrock globally. Written for Prototype stage only but applied globally. Pipeline stages where conversation IS the work product lost all earlier context.
**Fix:** Cap removed entirely. Full conversation history sent on every turn.
**Rule:** Never truncate conversation history for P01-P10. Prototype is the only exception. Prove with integration test.

### Bug 15: Phase definitions drifted from live prompt structures
**Discovery:** Sidebar showed wrong phase names and counts (13 phases for P01 which had 7).
**Root cause:** PhaseDefinitions is a hardcoded static dictionary never updated when prompts changed.
**Fix:** Updated P01/P03/P06/P09/P10. Added PhaseDefinitionAccuracyTests (9 tests) verified against live prompt files.
**Rule:** Every prompt phase structure change must update PhaseDefinitions in the same commit.

### Bug 16: Parking lot delete used wrong conversation ID
**Discovery:** Delete returned 404. Item existed but belonged to a different conversation.
**Root cause:** Items loaded at project level (all conversations) but deleted using current conversation ID. ParkingLotItemResource.conversationId existed but was ignored.
**Fix:** onDelete now receives full ParkingLotItemResource and uses item.conversationId.
**Seam type:** Project-level load + conversation-level mutation = ownership mismatch. Always carry the owning identifier through to the mutation call.

### Bug 17: API client used PUT but controller expected PATCH
**Discovery:** Edit notes and edit decisions returned 405 Method Not Allowed.
**Root cause:** projectNotesApi and projectDecisionsApi used apiClient.put but controllers use [HttpPatch]. No integration test making a real HTTP call to catch the mismatch.
**Fix:** Changed put to patch. Updated tests and mock setup.
**New seam type:** API client HTTP verb must match controller HTTP verb. Unit tests on either side cannot catch this — only a real HTTP integration test will.
**Rule:** When adding or modifying an endpoint, grep the controller to verify the verb before writing the client call. Never rely on memory.

### Bug 18: GitHub services threw in constructors when env vars absent
**Discovery:** Session-close crashed with 500 in dev. GitHubAppTokenService threw in constructor when GITHUB_APP_ID not set.
**Root cause:** Constructor-level throws propagate through DI resolution and crash the entire request before any try/catch fires.
**Fix:** AddGitHubIntegration checks GITHUB_APP_ID at registration time. When absent registers no-op implementations.
**Rule:** Side-effect services must never throw in constructors when optional config is absent. Register no-ops via DI. Primary operation must always complete regardless of side-effect service availability.
