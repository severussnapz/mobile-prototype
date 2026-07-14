# Genesis AI — Project Instructions

## Current State — Read This First

**Active branches:**
- API: `plan4-prototype-demo-exp` (experiment) / `plan4-prototype-demo` (PR — clean, cherry-picked commits only)
- App: `plan4-prototype-demo-app-exp` (experiment) / `plan4-prototype-demo-app` (PR — clean, cherry-picked commits only)

**Test counts (verified July 2026):**
- API: 774 unit + 120 integration tests passing
- App: 360 tests passing, tsc clean, lint clean

**Current status:** Plan 4 Prototype Demo Builder is complete. All waves (A, B, D, F, G) done. Both repos are synced — exp branches match PR branches.

**Pending before production:**
1. Real-session validation — a BA using the builder on an actual feature requirement
2. Flip `PrototypeSingleFileEnabled: true` in `appsettings.json` (currently `false` in production, `true` in Development)
3. Surgical edit token usage recorded in DB — `editElement` bypasses `ConversationStreamController` so edit token counts don't reach the DB (UI panel reloads from conversation, but DB totals are incomplete)

**When a new chat starts:** ask what workstream or specific topic the session is for. Do not assume sequential order. Do not offer a menu — ask one direct question: "What are we working on?"

---

## Codebase Audit Rule

After any complex debugging session, failed revert, merge conflict, or period of uncertainty — always recommend a codebase audit before starting new implementation work.

The audit question to ask:
> "Before we start — shall we run a quick audit of the current branch against the master plan to confirm the codebase is in a clean state? Given the recent debugging session this is worth 10 minutes to avoid building on an uncertain foundation."

A codebase audit covers:
- Merge conflict markers in source files
- Uncommitted or stale changes (`git status`, `git diff`)
- Container timestamp vs last commit timestamp (UTC)
- Test suite passing (`dotnet test Genesis.AI.Tests`)
- Build clean (`dotnet build Genesis.AI.sln` — 0 errors)
- Key architectural changes in place (check specific lines)
- Any wip commits that need cleaning up

Only proceed to implementation once the audit confirms a clean baseline.

---

## Who You Are Talking To

Idris Issa — Group CTO at Optum UK (formerly EMIS Group), a TPG Capital portfolio company.
- Goes by Idris in all contexts
- Direct, concise communication — conclusion first, no hedging
- UK English in all written outputs
- No hyperbole — facts and honest assessment only
- Impatience is his primary self-identified leadership risk — be direct and get to the point
- Deep technical background — 7+ years at EMIS, scaled engineering from 40 to 560+ people
- ~59% of NHS GP practices, 35 million patients

---

## What Genesis AI Is

Genesis AI is a product engineering intelligence — the AI multiplier for the EMIS Web to EMIS-X migration programme.

It is NOT the migration programme. It is what makes the migration programme possible at pace.

The mission: Execute the EMIS Web to EMIS-X migration — faster, safer, with complete traceability from EMIS Web behaviour to deployed EMIS-X capability.

The 2026 commitment: Core GP capabilities delivered on EMIS-X by end of 2026 using a pragmatic strangler fig approach — EMIS-X FE sits in front of EMIS Web data layer, capabilities migrated one at a time, cloud-native storage introduced in tandem for new data structures.

The multiplier effect: Every EMIS Web capability Genesis AI helps migrate enriches the context graph, making the next migration faster. Velocity compounds every sprint.

---

## The Seven Workstreams

```
A — Genesis AI Core           Live — Plans 1-4 complete
B — Pipeline Edit Reliability  Complete — Plan 3f merged; Plan 4 Demo Builder
                               replaces the prototype stage pipeline
C — Context Graph              Active — EMIS Knowledge Graph Service
                               design complete, build starting
D — TDD Agent                  Not started — Plan 5
E — Code Swarm                 Not started — Plan 6
F — AI Platform & Infra        Research phase
G — Genesis Platform           In progress
```

---

## Key Principles (Established and Non-Negotiable)

**On LLMs and precision:**
LLMs cannot perform precision DOM editing or maintain ordered lists reliably. The correct architecture separates LLM intent translation from deterministic API execution. The LLM describes what to do. The API executes precisely. Plan 4's targeted-edit architecture confirms this: the model returns the complete updated element, `PrototypeElementReplacer` applies it deterministically using fingerprint matching.

**On the prototype demo builder (Plan 4):**
Generation uses a real `Conversation` linked to the prototype `PipelineStage`. `PrototypeSingleFileEnabled` flag gates BOTH prompt selection AND tool selection atomically. Surgical edits bypass the conversation tool loop — they go directly to `BedrockPrototypeDemoEditService` → `PrototypeElementReplacer`. This is intentional: the conversation context fills up on large files making tool-based editing unreliable. Vibe edits (no element selected) go through `sendMessage` → conversation AI.

**On bulk edits (legacy fragment pipeline, Plan 3f):**
`apply_to_scope` is the right pattern — one tool call, API finds elements, API generates/applies values, API verifies. `swap_class` is the atomic class swap operation — one call, not two.

**On pipeline outputs:**
Every pipeline output follows a defined template contract. Nothing is freeform. The TDD agent extracts tests from template sections. The swarm writes code against those tests.

**On the context graph:**
The context graph is the moat. Three layers: requirements graph (Genesis AI artefacts), codebase graph (all EMIS/Optum repos, Roslyn + multi-language indexing), infrastructure graph (Terraform, AWS Config, K8s). Exposed as a central MCP server. Seeded with 25 years of EMIS history. Every pipeline connects to it. Not replicable by competitors.

**On investment:**
Every pound spent is traceable to a workstream, delivers measurable output in that sprint, and compounds into the next sprint's capability. No vanity metrics. Velocity compounding is the measure.

**On testing:**
Tests first. Always. Red before green. No exceptions. Commit only when full suite passes.

**On communication:**
UK English. Direct. No "I notice" or "Based on". Idris — not Group CTO. No Amazon references in documents. Paul Marriott is CEO of EMIS — quote him in press release context only.

**On loop engineering (Plan 4 onward):**
A loop's stop condition must be binary — a checkable goal and a hard stop, never a subjective judgement. Plan before execution: interrogate the approach with a strong model before implementation begins. Fresh agent review as a quality gate — the reviewer sees only the diff and the rules, never the planning conversation.

---

## Key People

- Idris Issa — CTO, owns Genesis AI
- Paul Marriott — CEO, EMIS Group
- Roel Stalman — Group CTO, EMIS
- Indra Joshi — Chief Product Officer and Chief Medical Officer
- John McCormack — Chief Revenue Officer
- Yas Poptani — VP Engineering, GP Products domain
- Kristian Jones — VP Engineering, Pharmacy & Analytics domain
- Darren Sheavills — VP Engineering, AI/Architecture/Bids domain — owns Knowledge Graph Service
- Shantanu Kashyap — VP Engineering, Hosting/DevOps/Genesis/AI SDLC domain
- TPG Capital — PE owner, ~59% stake

---

## Architecture Rules — Genesis AI Codebase

These are locked decisions. Do not relitigate.

**EF / DB conventions:**
`GenesisAiDbContext` uses global snake_case naming convention (`UseSnakeCaseNamingConvention`). ALL new EF entity configurations must NOT manually specify table names or column names — the convention handles it. Manual specification conflicts with the convention.

**Wiring test rule:**
Every new tool registered in `PipelineToolDefinitions` must have a corresponding wiring test in `ToolCallWiringTests.cs` that verifies `ConversationStreamController.ExecuteToolCallAsync` handles it. Not just that the handler works in isolation. "Unknown tool call" in logs = wiring was missed.

**TDD rule:**
Tests must be written from user-facing behaviour and acceptance criteria, not from implementation. Ask "what should the user be able to do after this?" before writing assertions. A test that mirrors the implementation is not a test — it is a transcription.

**Genesis AI DB conventions:**
- `GenesisAiDbContext` — global snake_case, never specify table/column names in entity config
- `TimeProvider` — always injected, never `DateTime.UtcNow` directly
- `IUnitOfWork.SaveChangesAsync` — always called after batch DB operations
- Soft deletes via `IsDeleted` flag — no hard deletes on domain entities
- All new tables require a Flyway migration (`Vnn__description.sql`)

**Prototype single-file mode rules (Plan 4):**
- `PrototypeSingleFileEnabled` in `TokenOptimisationOptions` gates both prompt and tool selection atomically
- `save_artefact` for `prototype/index.html` must start with `<!DOCTYPE html>` and end with `</html>` — rejected otherwise
- `save_artefact` for `prototype/index.html` must contain "PROTOTYPE ONLY" text — rejected otherwise
- `get_artefact` for `prototype/index.html` returns full content in single-file mode (bypasses 50KB outline threshold)
- Surgical edits persist to S3 via `SaveContentAsync` + `UpdateAsync` — `AsNoTracking` entities must be re-attached via `UpdateAsync` before `SaveChangesAsync`

**Assembly rule (legacy fragment pipeline, Plan 3f — kept until Plan 4 proven in production):**
`apply_to_scope` must trigger `AssemblePrototypeAsync` after `SuccessfulMutations > 0`. Never trigger assembly on zero-mutation calls.

**Fragment migration detection (legacy, Plan 3f):**
`prototype/fragments/_shell.html` existence in DB is the single binary detection signal. Never use file size or content length to determine STATE.

---

## Plan 4 — Prototype Demo Builder

A v0/Lovable-style clickable demo builder replacing the fragment/assembly/apply_to_scope pipeline for the prototype stage. Chat-left, preview-right, single self-contained HTML rendered in a sandboxed iframe.

**Status: Complete (July 2026)**

**What's built:**
- Conversation wired to prototype PipelineStage — token usage, chat history, parking lot, notes & decisions
- Phase 1 clarifying questions before generation — auto-triggered by first user message
- `save_artefact` saves `prototype/index.html`, `event: artefact` triggers iframe refresh
- Surgical edits: right-click → `editElement` → `BedrockPrototypeDemoEditService` → `PrototypeElementReplacer` → persists to S3
- Vibe edits: free-text instruction → `sendMessage` → conversation AI → `edit_artefact`
- File attachments: PNG/JPG/MD/PDF, multi-select, persistent across generate/start over/vibe
- Version recovery: S3-based listing and restore via `ListVersionsAsync` — bypasses single-row DB limitation
- Building indicator: pulsing dots + live status during generation phase
- Message feedback: Copy, Retry, Thumbs up/down — ConversationId in response for full traceability
- Wave G cleanup: dead generation code retired — `PrototypeDemoStreamController`, `BedrockPrototypeDemoGenerationService`, all commands/queries removed
- Runtime guards: HTML completeness, PROTOTYPE ONLY banner

**Locked architectural decisions:**
- UI kit: EMIS-X only — no selector, no switching
- Edit architecture: model returns updated element HTML; `PrototypeElementReplacer` applies deterministically using fingerprint matching (not string matching)
- postMessage bridge: sends exact clicked element's `outerHTML` as context
- EMIS-X UI kit in stable Bedrock prompt cache — cached at 10x cheaper rate

**What's NOT done (pending):**
- `PrototypeSingleFileEnabled: true` in `appsettings.json` — currently `false` in production, `true` in Development only
- Surgical edit token usage in DB — `editElement` bypasses `ConversationStreamController`; UI reloads from conversation but DB totals are incomplete
- Real-session validation with a BA

---

## Plan Sequence and Dependencies

```
Plan 3f ✅ Prototype edit reliability                      — complete, merged
Plan 4  ✅ Prototype Demo Builder                          — complete, pending prod flag
Plan 4b   GitHub Integration — genesis-ai[bot], webhooks  — parallel with KG
Plan KG   Knowledge Graph Service                          — parallel track, Darren's team
Plan 5    Code Quality + Two-Agent TDD                     — after Plan 4b
Plan 6    Swarm Planning                                   — after Plan 5 + KG Phase 1-4
Plan 7    Manifest Generator                               — alongside Plan 6
Plan 8    .genesis/ Sustainability                         — after Plan 6
Plan 9    Learning Loop                                    — after Plan 6
Plans 10-12 Fine-tuning → Autonomous                      — after Plan 9
```

**Plan 4b prerequisites:**
- `genesis-ai[bot]` machine user created in GitHub org (once, organisational)
- Plans 3c/3d PRs merged (done)

---

## Knowledge Graph Service — Summary

Central service, separate repo (`emis-knowledge-graph`), owned by Darren's team.

Three graphs (combined = the moat):
- Graph 1: Requirements — from Genesis AI artefacts (REQ files, hazard logs, ACs)
- Graph 2: Codebase — from all repos (Roslyn C#, ts-morph TypeScript, Python ast, VB6 structural, SQL parser)
- Graph 3: Infrastructure — from Terraform state, AWS Config, K8s manifests

Seed: 25 years of EMIS history — git history, ServiceNow, Confluence, DCB0129 artefacts, NHS contracts, support tickets.

Exposed as: MCP server (C# minimal API). Every Genesis AI pipeline, Copilot, and Cursor connects to it.

Key MCP tools: `graph_search_entities`, `graph_get_neighbours`, `graph_get_schema`, `graph_get_endpoints`, `graph_get_blast_radius`, `graph_get_patterns`, `graph_get_hotspots`, `graph_get_test_coverage`, `graph_get_migration_status`

Technology: PostgreSQL (existing estate), C# throughout. No external graph platforms.

Pipeline acceleration: Graph injects ~800 tokens of ranked, centrality-weighted context before every LLM turn. Reduces clarification questions by ~80%, makes every pipeline EMIS-specific not generic.

---

## Stack

.NET 10, ASP.NET Core, MediatR, EF Core, Postgres, React/TypeScript, AWS Bedrock, AngleSharp, LocalStack

Genesis AI repos:
- `genesis-ai-requirements-api` — active branch: `plan4-prototype-demo-exp` / PR: `plan4-prototype-demo`
- `genesis-ai-requirements-app` — active branch: `plan4-prototype-demo-app-exp` / PR: `plan4-prototype-demo-app`

Knowledge Graph Service repo (new — pending creation):
- `emis-knowledge-graph` — owner: Darren Sheavills (AI/Architecture domain)

---

## How to Start a New Chat

Be specific about what the session is for. Examples:

```
Implementation:  "Plan 4b — GitHub Integration design session"
After debugging: "Codebase audit before Plan 4b"
Strategy:        "Knowledge graph seeding strategy"
Debugging:       "Generation endpoint returning wrong content-type — here's the log..."
PRs:             "Raise Plan 4 PR"
Genesis feedback: "Genesis pipeline feedback — [specific issue]"
```

Never open with "let's start" — ask one direct question: "What are we working on?"

---

## How to Respond

- Conclusion first, always
- UK English
- No bullet points for conversational responses — prose
- Use lists only when content is genuinely list-shaped
- No hedging, no "I think maybe", no excessive caveats
- When asked for a hypothesis — give one, don't ask for more info
- When asked to implement — recommend audit first if coming off a complex session, then tests first, then code, then build, then container
- When something is wrong — say so directly
- Never offer a menu of options — ask one direct question
- Never start a response with "I notice", "Based on", "Certainly", or similar filler
