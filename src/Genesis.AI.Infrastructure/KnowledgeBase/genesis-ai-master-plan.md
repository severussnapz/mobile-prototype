# Genesis AI — Master Delivery Plan
Version: 4.3 — Updated July 2026
Owner: Idris Issa
Stack: .NET 10, ASP.NET Core, MediatR, EF Core, Postgres + pgvector, React/TypeScript, AWS Bedrock (PrivateLink), AngleSharp, LocalStack, ClosedXML

---

## The Methodology (Generic — Portable)

This section captures the universal pattern. It contains no organisation-specific references. When this methodology is applied in a new context, this section travels with it.

### What This Is
A product engineering intelligence that acts as an AI multiplier for complex platform migrations in regulated environments. It does not replace the engineering programme — it makes the programme faster, safer, and more traceable.

### The Core Pattern
Domain IP → Context Graph → Pipeline by Exception → 10x Velocity → Moat

Every organisation doing a complex platform migration has 10-30 years of domain knowledge embedded in their legacy system. That knowledge is the asset. The context graph formalises it. The pipeline reasons over it. Every capability migrated enriches the graph. Velocity compounds.

### The Seven Workstreams
```
A — Core Pipeline          The AI pipeline: requirements → prototype →
                           architecture → safety → IG → tests → code

B — Pipeline Edit          Precise, immutable, auditable changes to any
    Reliability            pipeline output at any stage.

C — Context Graph          Formalise domain IP into a structured,
                           queryable knowledge graph. Three graphs:
                           requirements, codebase (multi-repo), infrastructure.
                           Exposed as MCP server. This is the moat.

D — TDD Agent              Generate full test suite from approved pipeline
                           documents. Behavioural, safety, compliance,
                           integration tests — all derived, none written.

E — Code Swarm             AI swarm writes production code against
                           pre-generated tests. Guardrails enforced.
                           Human reviews PRs.

F — AI Platform &          Model selection, fine-tuning, tooling, IDE
    Infrastructure         integration, data sovereignty, guardrails.

G — Platform               Assured CI/CD, cloud deployment, IaC,
                           observability baseline.
```

### The Pipeline Pattern
```
Customer/stakeholder conversation
         ↓
Requirements capture (live, structured, approved in the room)
         ↓
Prototype generation (minutes, clickable, editable)
         ↓  ←── Feedback loop (GAP / CLARIFICATION / CONTRADICTION)
         ↓
Architecture (by exception against context graph)
         ↓
Regulatory/safety pipeline (by exception against existing work)
         ↓
TDD agent (tests from all approved documents)
         ↓
Code swarm (writes against tests, guardrails enforced)
         ↓
Deploy (real environment, real validation)
```

### The Editing Principle
LLMs cannot perform precision editing or maintain ordered lists reliably for bulk operations.
- LLM: translates human intent → structured operation (one call)
- API: finds elements → generates values → applies → verifies

The LLM describes what to do. The API executes with precision.

### The Output Contract Principle
Every pipeline stage produces output according to a defined template. The template defines required sections, format per section, quality gates, and what the TDD agent extracts from each section.

### The Strangler Fig Principle
- Separate the FE from the data layer first
- New experience sits in front of existing data — zero migration risk
- Migrate capabilities one at a time
- New data structures introduced in tandem on modern storage
- Legacy system remains system of record until each capability is proven
- Data migration follows proven capabilities — never precedes them

### The Graph Intelligence Principle
The context graph is not a search index. It is a structured map of relationships between business intent and running code. 800 tokens of the right knowledge outperforms 80,000 tokens of noise.

### Portability Note
To apply this methodology in a new organisation: replace the context graph sources with the new domain's legacy system, replace the regulatory pipeline with the relevant compliance framework, replace the stack with whatever is appropriate. The workstreams, principles, and patterns are unchanged.

---

## North Star Vision

Genesis AI is a product engineering intelligence that turns customer conversations into deployed, regulated software in days — not months.

A product person sits with a customer. Laptop open. Genesis AI running. They describe a problem. Genesis AI captures requirements in real time. A clickable prototype appears in minutes. The customer interacts with it — live, in the room. Requirements are approved before the meeting ends. A week later: real code, deployed, at the customer site.

---

## Status Summary (July 2026)

| Plan | Name | Status |
|------|------|--------|
| 1 | Token Refactor | ✅ COMPLETE |
| 2 | Skill Decomposition | ✅ COMPLETE |
| 3 | P02 Prototype Economics (fragment pipeline) | ✅ COMPLETE |
| 3a | Prototype Editing Hardening + Req Feedback Loop | ✅ COMPLETE |
| 3b | Prototype DOM Migration (AngleSharp) | ✅ COMPLETE |
| 3c | apply_to_scope Tool — Generic Bulk DOM Editing | ✅ COMPLETE |
| 3d | Requirement Change Feedback Loop | ✅ COMPLETE |
| 3e | Fragment Migration + swap_class + Prototype Skills | ✅ COMPLETE |
| 3f | Prototype Edit Reliability (smart search) | ✅ COMPLETE — merged |
| 4 | Prototype Demo Builder (v0/Lovable-style) | ✅ COMPLETE — pending prod flag |
| 4b | Knowledge Service + Help Chat Panel | ✅ COMPLETE |
| 4c | GitHub Integration + Platform Extensions | ✅ COMPLETE — PRs raised, awaiting merge |
| KG | EMIS Knowledge Graph Service | 📋 PARALLEL TRACK — Darren's team |
| 4d | Engineering Foundation | 🔄 IN PROGRESS |
| 4d-R | PR Review Agent | 📋 IN DESIGN — runs alongside 4d |
| 4e | Flow Spec (behavioural flow artefact) | 📋 IN DESIGN — P01→P02 seam, enriches P02/P06/TDD |
| 5 | Code Quality: Skills + Two-Agent TDD | 📋 PENDING — after 4d |
| 6 | Swarm Planning | 📋 PENDING — after 5 + KG Phase 1-4 |
| 7 | Manifest Generator + Executor | 📋 PENDING — alongside 6 |
| 8 | .genesis/ Repo Sustainability | 📋 PENDING — after 6 |
| 9 | Managed Learning Loop | 📋 PENDING — after 6 |
| 10 | Fine-tuning: Coding Model | 📋 PENDING — after 6 |
| 11 | Fine-tuning: Validation Model | 📋 PENDING — after 10 |
| 12 | Autonomous Loop | 📋 PENDING — after 11 |
| 14 | Cross-Req Coupling | ⏸️ DEFERRED — trigger condition not met |

---

## Plans 1–3f — Complete ✅

### Plan 1 — Token Refactor ✅
Foundation prefix caching, per-requirement windowing, non-windowed cross-check mode for P06/P07/P08. Key decisions locked: PROJECT FOUNDATION label is load-bearing, Category A/B/C artefact taxonomy, HAZ-ID watermark = orchestrator session variable never cached, per-requirement conversations (requirement_id FK on conversations table).

### Plan 2 — Skill Decomposition ✅
Thin agent cores (30-50 lines) + phase-aware skill injection + orchestrator-driven routing. Run modes: greenfield, additive, bugfix. Key decisions locked: run mode derived once at P01 from manifest presence, mandatory CSO/DPO/reviewer phases can never be suppressed, skills injected server-side before cache breakpoint.

### Plan 3 — P02 Prototype Economics (fragment pipeline) ✅
Fragment-based prototype generation + edit_artefact tool for surgical edits. ~5x token reduction per iteration. Key decisions locked: edit_artefact fail-closed with ANCHOR_NOT_FOUND/ANCHOR_AMBIGUOUS reason codes, assembly triggered server-side after every fragment save/edit, GENESIS: insertion markers in _shell.html are load-bearing.

### Plan 3a — Prototype Editing Hardening + Requirements Feedback Loop ✅
Unique DOM node IDs replace string anchors. Draft/published staging makes partial writes safe. Requirements feedback loop captures substantive UI/UX decisions from prototype interaction.

### Plan 3b — Prototype DOM Migration ✅
AngleSharp-based DOM search and mutation replaces graph-based node lookup. Removes the graph infrastructure dependency from the prototype pipeline.

### Plan 3c — apply_to_scope Tool ✅
Single tool call for bulk DOM mutations. Three strategies: literal, derive_from_text_content, generate_from_context. Assembly triggered after apply_to_scope only when SuccessfulMutations > 0.

### Plan 3d — Requirement Change Feedback Loop ✅
Agent-led domain impact classification for requirement changes. Changes surface in UI with domain badges (CS/IG/SEC). Tool: propose_requirement_change.

### Plan 3e — Fragment Migration + swap_class + Prototype Skills ✅
PrototypeFragmentMigrationService (pure C#, no LLM, deterministic). swap_class atomic operation (one call, not two). Three new skill files: prototype-state-detection.md, prototype-edit-discipline.md, prototype-build-discipline.md.

### Plan 3f — Prototype Edit Reliability ✅
Smart search and edit reliability hardening. Merged. Fragment pipeline stable before handover to Plan 4.

---

## Plan 4 — Prototype Demo Builder ✅ (pending production flag)

**What it is:** A v0/Lovable-style clickable demo builder replacing the fragment/assembly/apply_to_scope pipeline for the prototype stage. Chat-left, preview-right, single self-contained HTML rendered in a sandboxed iframe. Supersedes the fragment-based approach entirely for new prototypes.

**Status:** Complete (July 2026). Pending production flag flip.

**Architecture:**
- Generation: real `Conversation` linked to prototype `PipelineStage`. `PrototypeSingleFileEnabled` flag gates BOTH prompt selection AND tool selection atomically
- Surgical edits: right-click → editElement → BedrockPrototypeDemoEditService → PrototypeElementReplacer (fingerprint matching). Bypasses conversation tool loop intentionally — context fills up on large files
- Vibe edits: free-text → sendMessage → conversation AI → edit_artefact or save_artefact
- Token usage: recorded for both generation and surgical edits via RecordSurgicalEditTokenUsageAsync

**What's built (verified July 2026):**
- Conversation wired to prototype PipelineStage — token usage, chat history, parking lot, notes & decisions
- Phase 1 clarifying questions — auto-triggered by first user message
- save_artefact saves prototype/index.html, event: artefact triggers iframe refresh
- Surgical edits persist to S3 via SaveContentAsync + UpdateAsync
- File attachments: PNG/JPG/MD/PDF, persistent across generate/start over/vibe
- Version recovery: S3-based listing and restore via ListVersionsAsync
- Building indicator, message feedback (Copy/Retry/Thumbs), chat history persistence
- Runtime guards: HTML completeness, PROTOTYPE ONLY banner
- Wave G cleanup: dead generation code retired

**Locked architectural decisions:**
- UI kit: EMIS-X only — no selector, no switching
- Edit architecture: model returns updated element HTML; PrototypeElementReplacer applies deterministically using fingerprint matching
- postMessage bridge: sends exact clicked element's outerHTML as context
- EMIS-X UI kit in stable Bedrock prompt cache — cached at 10x cheaper rate

**Pending before production:**
- Flip PrototypeSingleFileEnabled: true in appsettings.json
- Anchor failure root cause resolved — surgical edit fingerprint mismatches (see Plan 4d)

**Wave H (after production flag flip):**
- Figma Option A — IFigmaImageService calls Figma /v1/images/{file_key}, returns PNG, feeds into existing vision input path. PAT stored as project-level secret

**Note on legacy fragment pipeline:** Plans 3b/3c/3e (apply_to_scope, swap_class, AngleSharp DOM pipeline) remain active in production until Plan 4 is proven in real sessions and the production flag is flipped.

---

## Plan 4b — Knowledge Service + Help Chat Panel ✅ COMPLETE

**Status:** Complete (July 2026).

**What was built:**
- Genesis AI Knowledge Service — pgvector inside genesis-ai-requirements-api. Two namespaces: genesis-tool (Genesis AI pipeline docs, global, seeded on deployment) and project-artefact (approved artefacts per project, indexed at approval time)
- IKnowledgeService interface — clean swap to Workstream C Knowledge Graph MCP when ready
- ArtefactPublishedDomainEvent handler indexes artefacts on approval
- HelpConversation aggregate — lightweight, DB-persisted, no StageId, ProjectId? and UserErn only
- HelpChatController — stream endpoint + conversation history
- HelpChatStreamService — queries both namespaces per turn, injects into Bedrock system prompt
- Help Chat Panel — persistent floating panel at Shell level, available from every page
- User guide indexed into genesis-tool namespace on deployment

**Migrations:** V18 (pgvector) ✅ V19 (knowledge_document) ✅ V20 (help_conversations + help_messages) ✅
**Workstream C plug-in point:** IKnowledgeService → KnowledgeGraphMcpClient. DI swap only.

---

## Plan 4c — GitHub Integration + Platform Extensions ✅ COMPLETE (PRs raised, awaiting merge)

**Status:** Feature-complete (July 2026). PRs raised and reviewed. Awaiting merge.

**What was built:**
- D1 ✅ AesSecretEncryptionService, GitHubAppTokenService, GitHubContentsService — GitHub App token, AES-256-GCM encryption, Contents API client
- D2 ✅ P00 form extension — V21/V22 migrations, three focused PATCH endpoints (/details, /github, /p00), UpdateProjectGitHubCommand/DetailsCommand/P00Command
- D3 ✅ GenesisStructureScaffolder — pushes 11 files to .genesis/, idempotent, CODEOWNERS, PROJECT.md
- D4 ✅ GitHubArtefactPushService — post-approval artefact push, push_failure_log (V23), github_pushed_at (V24), ArtefactPublishedDomainEvent extended
- D5 ✅ SESSION-CLOSE endpoint — GenerateSessionCloseCommandHandler, upserts per stage, isPublished: true triggers GitHub push
- D6 ✅ P06 Excel hazard log — on-demand via existing button (not auto on approval)
- D7 ⏸️ P06 hazard tracking DB API — parked pending CS team API schema
- Push-to-GitHub UI ✅ — bulk push button, per-artefact Push button, GitHub status column, filter
- Settings tab ✅ — Project Details, GitHub Configuration, P00 Configuration, push-status badge

**Migrations:** V21 ✅ V22 ✅ V23 ✅ V24 ✅
**genesis-ai-bot bypass:** Added to emisgroup "Force Pull Request on Main" org ruleset.

---

## EMIS Knowledge Graph Service 📋 PARALLEL TRACK

**Owner:** Darren Sheavills (AI/Architecture domain)
**Repo:** emis-knowledge-graph (new standalone repo)
**Status:** Design complete, build starting

**Three graphs:**
- Graph 1 — Requirements: from .genesis/ artefacts across all feature repos. Fed nightly from GitHub. Approval action is the data feed
- Graph 2 — Codebase: from all repos via Roslyn C#, ts-morph TypeScript, Python ast, VB6 structural parsing, SQL parser
- Graph 3 — Infrastructure: from Terraform state, AWS Config, K8s manifests

**Seed:** 25 years of EMIS history — git history, ServiceNow, Confluence, DCB0129 artefacts, NHS contracts, support tickets
**Exposed as:** MCP server (C# minimal API). Every Genesis AI pipeline, Copilot, and Cursor connects to it.
**Key MCP tools:** graph_search_entities, graph_get_neighbours, graph_get_schema, graph_get_endpoints, graph_get_blast_radius, graph_get_patterns, graph_get_hotspots, graph_get_test_coverage, graph_get_migration_status
**Technology:** PostgreSQL (existing estate), C# throughout. No external graph platforms.
**Pipeline acceleration:** Graph injects ~800 tokens of ranked, centrality-weighted context before every LLM turn. Reduces clarification questions by ~80%. Makes every pipeline EMIS-specific not generic.

**Build phases:**
- Phase 1 (2 weeks): Core infrastructure — V1 schema, GraphNode/GraphEdge entities, RoslynIndexer, NightlyIndexerJob, degree centrality, core MCP tools
- Phase 2 (1 week): MCP server — graph_search_entities, graph_get_neighbours, graph_get_schema, graph_get_endpoints
- Phase 3 (1 week): Additional language indexers — TypeScript, Python, SQL
- Phase 4 (1 week): Genesis AI integration — MCP client, feature flag, per-pipeline anchor extraction, P03/P04 integration
- Phase 5 (ongoing): Advanced tools + 25-year seed — blast radius, patterns, hotspots, VB6 indexer, ServiceNow, DCB0129 seeding

**Genesis AI plug-in point (Plan 4b → Plan KG):** IKnowledgeService in the help chat is swapped for the Knowledge Graph MCP client when Phase 4 is ready. No rearchitecting.

---

## Mission 30 Retro Actions — Status Update (July 2026)

**Source:** Mission 30 Retro — 23 feedback items from teams using raw Copilot prompts.
**Context:** All feedback was generated by teams NOT using Genesis AI. The majority are closed by migration to Genesis AI.

| Action | Description | Status |
|---|---|---|
| Action 1 | Help Chat Panel | ✅ COMPLETE — Plan 4b |
| Action 2 | User Guide | ✅ COMPLETE — indexed into genesis-tool namespace |
| Action 3 | SESSION-CLOSE Button (all stages) | ✅ COMPLETE — Plan 4c D5 |
| Action 4 | Figma Option A integration | 📋 Wave H — after PrototypeSingleFileEnabled prod flip |
| Action 5 | Artefact push to feature repo (.genesis/) | ✅ COMPLETE — Plan 4c D4 |
| Action 6 | P00 project setup form extension | ✅ COMPLETE — Plan 4c D2 |
| Action 7 | P06 Excel export artefact | ✅ COMPLETE — on-demand via existing button |
| Action 8 | P06 DB API integration | ⏸️ PARKED — CS team API schema not yet defined |
| Action 9 | CODEOWNERS file + prompt governance | ✅ COMPLETE — Plan 4c D3/D8 |
| Action 10 | Pipeline chat cross-stage artefact access | ✅ COMPLETE — cross-stage artefact-access section added to all P01–P10 prompts (exp) |
| Planned 1 | Project Dashboard (KPIs and OKRs) | 📋 PENDING — design session required (Idris, Yas, Roel) |
| Planned 2 | Medical Device Pipeline (P09) | 📋 PENDING — design session with Indra required |

10 of 10 engineering actions complete or in progress. Action 8 parked (CS team API). Design sessions (Planned 1, 2) pending.

---

## Plan 4d — Engineering Foundation 🔄 IN PROGRESS

**What it is:** A focused engineering improvement sprint to close the contract integrity gaps exposed by the Plan 4c container test, plus the contract layer design and guardrail infrastructure that Plan 5 depends on.

**Why it exists:** The Plan 4c container test revealed seven classes of bug that unit tests did not catch: missing response model field mappings, enum serialisation mismatches, missing controller actions, form state contamination, EF column mapping gaps, frontend/backend property name mismatches, and full aggregate loads for single-field updates. Additionally, three silent-seam failures were caught during July 2026: the DTO mapping gap (fields computed but never reaching the HTTP body), the SESSION-CLOSE write-only artefact (generated but never re-injected on resume), and the missing PATCH route class. These are structural gaps that compound as Plan 5 adds complexity.

### Completed (July 2026)

**Contract layer design — complete, implementation started**
- `contract-layer-design.md` committed to KnowledgeBase — full design covering contract definition (4 plain-text files under `.genesis/design/`), versioning via existing per-filePath mechanism with a `CONTRACT.md` manifest pinning a coherent set, resumption/staleness model reusing CHANGE-record domain badges, enforcement via per-turn prompt rebuild (verified against real code), tagging governance across CS/IG/SEC (P04 drafts mechanically, role-holders ratify at P06/P07/P08 as by-product of existing assessment), TDD gate (strict form — manifest pins REQ+ARCH provenance), tag vocabulary (stable `tagId` identity, renames surfaced as human-confirmed events via existing parking-lot/CHANGE machinery), and guardrail set (five seam-test types targeting the silent-seam failure class).
- Contract manifest aggregate: `ContractManifest` + `ContractManifestPin` entities, `ContractPinRole` enum, V25 migration, `IContractManifestRepository`, EF configs — all committed. Pins are `(role, filePath, version)` value records — fully-qualified references, not bare ints. Factory enforces exactly one pin per role; required set derived from enum so it auto-tracks new roles.

**SESSION-CLOSE re-injection fix — complete**
- Gap confirmed: SESSION-CLOSE artefacts were generated, stored, and pushed to GitHub but never read back into the prompt on resume. Write-only artefact.
- Fix: `ISessionCloseContextBuilder` / `SessionCloseContextBuilder` — reads latest published SESSION-CLOSE artefact for the current stage and injects it into the mutable part of the per-turn prompt rebuild.
- Integration test (guardrail 3, stronger form): write → resume → assert content present in rebuilt prompt.
- First instance of the injection-contributor pattern the contract enforcement will reuse.

**Copilot anti-shortcut rules — committed to repo**
- Five rules in `.github/copilot-instructions.md`: no optional/nullable dependencies with null-object fallbacks; no warning suppression; no test assertion changes to force green; confession-language self-audit; no build-configuration edits to route around compile errors.
- Three Copilot cheats caught and reversed live (null-object bypass, CA1859 suppression, build-props global-using hack) — all GREEN, none shipped.

**Seam guardrail set — designed and documented**
- Five seam-test types defined: result→HTTP body, command→route, artefact write→read-back (stronger form), tool registration→wiring, pin→resolution.
- Standing rule: a new class of seam failure means a new seam-test type in the family — never just fixing the instance.

**SDLC skills library — committed to KnowledgeBase and Infrastructure/Skills/**
- 19 senior-expert SDLC skills across two bundles committed to KnowledgeBase.
- 5 pipeline-agent skills committed to `Infrastructure/Skills/` and wired into `PhaseSkillMap`:
  - `agent-discipline` — universal, all stages, all phases
  - `requirements-elicitation` — P01, all phases
  - `api-contract-design-craft` — P04 phase 1
  - `seam-testing` — in folder, unwired (ready for Plan 5)
  - `review-agent-discipline` — in folder, unwired (injected via Review Agent prompt)
- P01 early-return exclusion removed from PhaseSkillMap — RequirementsDiscovery now receives skills.

**Both PRs fixed and re-reviewed**
- API PR: IAiService `StreamWithToolsAsync` collapsed to single optional-param method; 7 integration mocks updated; NHS guard error message fixed.
- App PR: lint clean; aria-label i18n; emoji removed from visually-hidden spans; sandbox iframe test split.
- Re-review comments posted on both PRs. Awaiting merge.

**Test counts (verified July 2026):**
- API: 942 unit + 128 integration
- App: 362 tests, tsc clean, lint clean

### Remaining items (all must be checked before Plan 5 starts)

**Production gates:**
- [x] Bulk push button noop fixed ✅
- [x] Session-close button noop fixed ✅
- [x] BA validation session on a real feature requirement ✅
- [ ] `PrototypeSingleFileEnabled: true` flipped in production
- [ ] Anchor failure root cause resolved — surgical edit fingerprint mismatches, fix root cause not symptom
- [ ] Both PRs merged to main

**Contract layer implementation (contract-layer-design.md §10 items 2–6):**
- [ ] Create command — reads current approved REQ + ARCH versions, validates pinned artefacts exist, writes manifest
- [ ] Staleness check + injection — per-turn rebuild, feeds existing `stalenessNotice`
- [ ] Error catalogue — `ERROR-CATALOGUE.md` as P04 output, versioned, frontend reads from it
- [ ] Tagging — P04 draft pass, traceability section, P06/P07/P08 ratification worklist
- [ ] TDD gate — strict form, REQ+ARCH provenance, blocks Plan 5 start
- [ ] Guardrail suite — five seam-test types
- [ ] PR Review Agent wired into pipeline pre-commit gate (see Plan 4d-R)

**Engineering hygiene:**
- [ ] DTO mapping completeness structural fix — vertical slice from UpdateProjectGitHub as reference implementation; every result field requires response DTO + controller mapping + mapping test; backfill all existing response models
- [ ] Systemic error handling audit — every existing endpoint audited for silent failures; two-tier pattern (Tier 1 ProblemDetails + userMessage, Tier 2 push_failure_log) enforced by default not by exception; guardrail tests added
- [ ] NSwag type generation configured
- [ ] Controller completeness check in all Copilot prompt templates
- [ ] JSON field name integration tests for all response models
- [ ] Behavioural tests for same-turn edit guard, post-search read block, zero-match block (ToolExecutionContext prerequisite)
- [ ] ToolExecutionContext extraction

**Pipeline Prompt Quality Review — full P01–P11 audit:**
- [ ] Anti-rationalization tables added to every Skills file (ref: https://github.com/addyosmani/agent-skills)
- [ ] Binary stop conditions — replace all subjective exit criteria with checkable binary conditions
- [ ] Stage-by-stage optimisation — P01–P11, prompt structure, phase sequencing, question quality, output template completeness, token efficiency
- [ ] Cross-stage traceability audit — HAZ-IDs → P06, ADRs → P03, CHECKs → P11
- [ ] Doubt-driven development gate — formalise CLAIM → EXTRACT → DOUBT → RECONCILE across all stages
- [ ] LLM/script boundary audit — for each P01–P11 stage prompt, ask: "What is the LLM doing here that a script should do?" Flag any instance where the prompt asks the LLM to fetch, extract, parse, or construct something the stage handler could pre-process and inject as structured data. Known instance: P08→P09 pre-swarm assembly — open decisions to be queried from the artefact DB and injected as a formatted list, not mined from raw artefact text. Output: one findings note per flagged stage; fixes committed before Plan 5.

**Known gap — large-artefact `get_artefact` returns outline, not full content (surfaced in use, July 2026):**
- [ ] Observed live in a P06 Clinical Safety resume: `get_artefact` on `HAZARD-REGISTRY.md` returned only the ~2,251-char structural outline, not the full hazard cards. The agent could not load the complete registry and fell back to the SESSION-CLOSE record to proceed.
- [ ] Root cause: the 50KB outline threshold that returns a structural summary for large files is bypassed ONLY for `prototype/index.html` in single-file mode (the `prototypeSingleFile` param on `BuildGetArtefactResult`). Every other artefact — including the clinical hazard registry — still receives the truncated outline once it exceeds the threshold.
- [ ] Impact (clinical-safety, HIGH): the CSO agent scores hazards without being able to see all existing hazard cards — risk of duplicate HAZ-IDs and missed hazards. The agent itself flagged identifier integrity (OI-P06-004) as unresolvable without the full sequence.
- [ ] Fix direction: proper resolution is the structure-aware chunker + small-to-big retrieval from the Knowledge Layer plan (match small, inject the parent section) so an agent pulls a specific hazard card by heading path rather than a whole-file outline. Interim option: extend the full-content bypass to registry-class artefacts the pipeline reads whole. Needs a real large-file retrieval test — not the InMemory-tested path.

**Known gap — help chat retrieves wrong chunk on vague follow-up queries (surfaced in use, July 2026):**
- [x] Observed live: “why are we doing it” retrieved DCB0129 content instead of Artefact Scope Restructure. Root cause confirmed: no conversation history passed to retrieval; each query hits vector store cold.
- [x] **Interim fix COMPLETE (July 2026):** `BuildRetrievalQuery` added to `HelpChatStreamService` — prepends the most recent prior user message (ordered by `CreatedAt`) as `"{prior}: {current}"` before both `QueryAsync` calls. No LLM round-trip, no new dependency. 5 tests passing. 960 unit + 95 analyser rules passing.
- [x] **Re-index knowledge endpoint + button COMPLETE (July 2026):** Root cause of poor project retrieval confirmed — `ArtefactPublishedDomainEventHandler` is best-effort; if Bedrock unavailable at publish time, artefact saves but is never indexed. Documents Management Core had 9 chunks (one SESSION-CLOSE) despite 19 published artefacts. `POST /api/v1/projects/{projectId}/artefacts/reindex` + Re-index knowledge button added to artefacts tab. After re-index: 780 chunks. Project-specific help chat answers now correct.
- [ ] Remaining: durable fix for vague follow-ups is the structure-aware chunker + small-to-big retrieval (Knowledge Layer Phase 3). Interim anchoring helps but cannot overcome dense clinical safety indexing on generic queries. Specific questions now get specific answers.

**Effort remaining:** ~2 weeks.
**Owner:** Idris
**Gate:** Plan 5 cannot start until all items above are checked off.

---

## Plan 4d-R — PR Review Agent 📋 IN DESIGN (runs alongside Plan 4d)

**What it is:** A structured, evidence-based review agent that gates code commits and PRs. Two activation points, one shared base prompt.

**Why it exists:** "Tests passing" proved insufficient — Copilot reached for shortcuts that went GREEN while hiding defects (null-object bypass, warning suppression, build-config edits). The Review Agent is the structural answer: a mandatory review of every diff against a rule set before code commits or merges.

### Architecture

**Three prompt documents committed to KnowledgeBase:**
- `review-agent-base.md` — shared foundation: seven review dimensions, finding format (Rule ID, severity, file:line, evidence, impact, fix, autofix), output structure (summary, blockers, important, polish, passing checks, final verdict, action checklist), behaviour constraints.
- `review-agent-genesis-pipeline.md` — P11 pre-commit gate: nine Genesis-specific rules (GENESIS-001 through GENESIS-009).
- `review-agent-github-ci.md` — GitHub Actions PR gate: CI-scoped Genesis rules, conventional commits, AWS Bedrock via PrivateLink, deference to pipeline gate for artefact-context checks.

### Point 1 — Genesis Pipeline Pre-Commit Gate

```
P11 generates code + tests
        ↓
Review Agent (review-agent-genesis-pipeline.md)
        ↓
APPROVE / APPROVE WITH COMMENTS → genesis-ai[bot] commits
REQUEST CHANGES → returned to P11 with findings as structured input
BLOCKED → human escalation, genesis-ai[bot] does not commit
```

Output: `REVIEW-{id}.md` artefact committed to `.genesis/review/` alongside the generated code.

### Point 2 — GitHub CI/CD PR Gate

```
Developer pushes / genesis-ai[bot] commits
        ↓
Review Agent (GitHub Actions, review-agent-github-ci.md)
        ↓
APPROVE → required status check passes, PR can merge
BLOCKED → status check fails, PR blocked
```

Required status check name: `genesis-review-agent`. Model: AWS Bedrock via PrivateLink.

### Nine Genesis-Specific Rules (GENESIS-001 through GENESIS-009)

| Rule | Severity | What it catches |
|---|---|---|
| GENESIS-001 | Critical | Optional/nullable dependencies with null-object fallbacks |
| GENESIS-002 | Critical | Warning suppression (NoWarn, #pragma disable) |
| GENESIS-003 | Critical | Build-config edits to route around compile errors |
| GENESIS-004 | Critical | Test assertion changes to force green |
| GENESIS-005 | High | Type erasure in test helpers (IReadOnlyList<object>) |
| GENESIS-006 | High | EF Core missing ToTable/HasColumnName mappings |
| GENESIS-007 | Critical | NHS data in logs, error messages, query strings, unencrypted fields |
| GENESIS-008 | High/Blocker | Missing seam tests for introduced seams |
| GENESIS-009 | High | Schema change without Flyway migration |

**Status:** Prompts designed and committed to KnowledgeBase. Pipeline wiring (Point 1) is a Plan 4d item. GitHub Actions wiring (Point 2) is a Workstream G item (Shantanu).

**Owner:** Idris (pipeline wiring), Shantanu (CI/CD wiring).

---

## Plan 4e — Flow Spec (Behavioural Flow Artefact) 📋 IN DESIGN (runs alongside Plan 4d)

**What it is:** An optional, per-requirement behavioural flow artefact at the P01→P02 seam. The agent drafts a flow diagram from a plain-English description of how a feature behaves; the user audits and corrects it; on approval a structured flow model is persisted against the requirement and consumed downstream by P02 (prototype), P06 (clinical safety) and the TDD agent. The canvas is what you audit, not what you author.

**Why it exists:** P01 produces prose REQs with acceptance criteria; P02 produces screens. Nothing between them captures behavioural sequencing — step → decision → branch → loop — explicitly. Prose ACs describe sequencing and loops awkwardly; a clickable prototype shows the screens while hiding the branching logic. A clinician spots a missing branch in a flow diagram in seconds — they cannot spot it by clicking through five prototype screens. In a regulated context every decision node is where a clinical hazard hides, which makes the flow a direct DCB0129 (P06) input, not a cosmetic aid.

### The user loop
1. **Describe** — the user types the behaviour in plain English inside the P01 conversation.
2. **Draft** — the agent emits the structured flow model; a deterministic validator confirms structural completeness; the diagram renders. The user did not draw it — the agent did.
3. **Audit + correct** — the user reads it and corrects by plain-English instruction ("escalation should come before the video step") or by clicking a node and describing the change. No dragging, no palette, no building.
4. **Approve** — the approved flow persists against the requirement and feeds P02 and P06.

### Load-bearing decisions (everything else stands on these)
1. **Source of truth is a structured flow model, not the Mermaid.** The Mermaid markup is a generated rendering for humans; never persisted as master, never parsed downstream. Storing rendering DSL as master and mining structure back out of it is the P08→P09 anti-pattern (LLMs mining raw artefact text for structured data the DB could supply directly).
2. **Decision conditions are references to acceptance criteria, not copies.** A decision condition ("pain > 6 or refill request") *is* a binary AC. The node holds an `ac_ref`; the AC owns the truth. Editing a condition in the flow proposes an AC change via `propose_requirement_change` (Plan 3d) — never a silent second copy.
3. **Click-to-edit resolves by stable node id, never by rendered-text string match** — avoids `ANCHOR_AMBIGUOUS`, the Plan 3a lesson.
4. **Optional, keyed on graph-structure, enriches — never gates.** The agent offers a flow only when behaviour has structure a flat AC list cannot hold (sequencing, loops, convergence); a lone binary decision stays as two ACs. When a flow is absent, every downstream stage degrades gracefully to today's prose-AC behaviour. No downstream stage may hard-depend on a flow existing — that is the completeness check on the word "optional".
5. **One flow per requirement; the container is the requirement, not a canvas or a tab strip.** A flow is an optional child artefact hanging off `requirement_id`, inheriting the requirement's identity, versioning, staleness and change machinery. A pathway spanning two requirements lives with the requirement that owns its outcome; the hand-off is a terminal node referencing the other requirement's flow (`flow_ref`), never a merged canvas — the seam where deferred Plan 14 plugs in.

### The flow model (minimal — not a workflow builder)
Persisted as data in the DB (not S3 content); the human-readable `FLOW-{id}.md` rendering is pushed to GitHub for audit and never parsed.

```
FlowModel   flow_id (PK), requirement_id (FK, unique), version, entry_node_id, status (draft|approved)
FlowNode    node_id (stable), flow_id (FK), type (step|decision|terminal), label,
            ac_ref   (nullable — on decision nodes; references an AC id, NOT a copy),
            flow_ref (nullable — on terminal nodes; references another requirement's flow)
FlowEdge    edge_id (PK), flow_id (FK), source_node_id, target_node_id,
            branch_label (nullable — on edges leaving a decision node)
```
Loops are an edge whose target is an earlier node. Convergence is multiple edges into one node. No special constructs — it is a directed graph.

**Deterministic validation ("schema validates it" — script, not LLM):** exactly one entry node; every node reachable; every decision node has ≥2 outgoing edges and a resolvable `ac_ref`; every branch terminates; every loop is escapable; every `ac_ref`/`flow_ref` resolves. LLM drafts (judgment); the validator enforces completeness (mechanics).

### Downstream consumption — one model, many readers
- **P02 prototype** reads nodes + edges as states + transitions — steps become candidate screens, edges become navigation. A fourth structural input alongside REQ prose, the EMIS-X UI kit and the style reference.
- **P06 clinical safety** reads decision nodes + escalation terminals as hazard sites. Branch → candidate HAZ-ID.
- **TDD agent (Plan 5+)** reads paths — every entry-to-terminal path is a behavioural test case, every decision a branch-coverage obligation, every loop a termination test.
- **The REQ** reconciles conditions via `ac_ref` through the existing Plan 3d AC-insertion / `propose_requirement_change` machinery.

**Disciplines:** one model, many readers (no per-stage artefact — that would recreate the write-only failure class); the flow→downstream boundary is a seam (seam-test family treatment); Mermaid is never parsed downstream; conditions reference ACs; clicks resolve by node id.

### Deferred framing (named, not accidental)
Sibling child artefact (build now — reuses `requirement_id`, per-requirement windowing and the Plan 3d change machinery almost untouched) vs the requirement as a structured object whose behaviour is graph-shaped (cleaner — dissolves the two-masters tension — but a real change to the requirement model). **Build the sibling-artefact version now, with the condition-as-AC-reference rule baked in, and design the flow model so it can be promoted into the requirement structure later without a rewrite.**

### Dependencies
**Proven / reused (no new build):** per-requirement conversations + `requirement_id` FK (Plan 1); Plan 3d change machinery + `propose_requirement_change`; immutable S3 versioning + artefact push (Plan 4c); the Plan 4 chat-left / render-right + click-to-target interaction pattern.

**New build:** flow model tables + Flyway migration (explicit `ToTable`/`HasColumnName`); `draft_flow` and `edit_flow_node` tools (structural edits via the vibe path); deterministic structural validator; Mermaid render + node click-target surface (lighter than the prototype iframe); downstream projection readers for P02, P06 and the TDD agent; binding the flow to the Plan 3d change loop in **both** directions (a requirement change marks its flow stale; a flow condition edit proposes a requirement/AC change back).

### Status, owner, positioning
**Status:** 📋 IN DESIGN.
**Owner:** Idris.
**Positioning:** Not a Plan 5 blocker. Design now (alongside Plan 4d); build after the Plan 4 production flag flip (the render/edit surface must be proven in real sessions first), then run in parallel with the rest of Plan 4d and with Plan 5. The output enriches Plan 5 (flow paths → behavioural tests); Plan 5 does not hard-depend on it.
**Effort (indicative, pre-build — not committed):** ~2–3 weeks for the backend model + validator + `draft_flow` and the render/audit/approve loop (Phases 1–2). Downstream projections land incrementally: P02 and P06 first, the TDD projection with Plan 5.
**Resolved:** `FLOW-{id}.md` is co-located in `requirements/` alongside `REQ-{id}.md`, `CHANGE-{id}.md` and `TEST-{id}.md`. No separate `flows/` folder.

**Suggested phasing:**
1. **AC stable IDs (Option 1 — built properly, no shortcut)** + flow model + migration + deterministic validator + `draft_flow` (backend, no UI). The parser assigns `AC-{req_id}-{seq}` IDs; a migration adds the AC table; P01 persists ACs with stable IDs on save. Prerequisite for decision-node `ac_ref` **and** for the functional-test AC references in `TEST-{id}.md` (Plan 5). A data-model change, done once, correctly — the product must sing at launch.
2. Render surface + `edit_flow_node` + playback-before-save + approve (the user loop).
3. Downstream projections — P02, then P06, then the TDD path-to-test projection with Plan 5.

---

## Plan 5 — Code Quality: Skills + Two-Agent TDD 📋 PENDING

**Prerequisite:** Plan 4d complete (all items checked off).

**What it is:**
- response-discipline.md skill — eliminates preamble bloat
- efficient-implementation.md — anti-patterns checklist for coding agents
- debugging-discipline.md — hypothesis-first debugging protocol
- Two-agent TDD: Agent A writes tests from REQ CHECKs (cannot see implementation), Agent B makes them pass (cannot modify tests)
- TASK-NNN-TESTS and TASK-NNN-CODE as separate manifest entries
- TASK-NNN-CODE.can_start = false until TASK-NNN-TESTS is human_approved
- `seam-testing` skill (already in Infrastructure/Skills/) wired into P11
- `review-agent-discipline` skill wired into the Review Agent prompt at P11
- **Artefact Scope Restructure** — the REQ becomes a thin index; each stage's output moves to a dedicated per-project / per-requirement artefact; `TEST-{id}.md` per requirement (see sub-section below and `artefact-scope-restructure-design.md`)

### Plan 5 — Artefact Scope Restructure (REQ de-bloat + Test Registry)

Full design: `artefact-scope-restructure-design.md` (KnowledgeBase).

**The problem:** REQ files bloat because every stage (P03–P08) writes its full output into the REQ additively. Across twenty requirements this produces multi-thousand-line files that degrade retrieval, pressure downstream-agent context, and make change blast-radius opaque. The hazard registry already solved this for P06 (HAZ cards in `HAZARD-REGISTRY.md`, HAZ-ID references in the REQ). This generalises that pattern.

**The change:** each stage's full content moves to a dedicated artefact; the REQ becomes a thin index of references.

**Scope rule (the backbone):** a stage's output is **per-project** when one role ratifies it holistically across all requirements; **per-requirement** when it is scoped to one requirement's behaviour and consumed by an agent working on that requirement alone.

```
Per requirement:  REQ-{id}.md (index), FLOW-{id}.md, CHANGE-{id}.md, TEST-{id}.md
Per project:      ARCH.md, DESIGN.md, PXD.md, HAZARD-REGISTRY.md,
                  IG.md / DPIA.md, SECURITY-REGISTRY.md, PROJECT.md
```

**Load-bearing decisions:**
1. The REQ is an index, not a container — summary + reference only; if a stage's content is only findable in the REQ, the extraction is incomplete.
2. Scope is decided by who ratifies, not by convenience.
3. Traceability is bidirectional — the REQ references the project artefact; the project artefact section references back the `REQ-{id}`s it serves. Missing back-reference = P09 normalisation failure.
4. Staleness is section-scoped — a REQ change marks stale only the project-artefact sections that reference that requirement, not the whole file. The one new piece of machinery: a requirement-reference staleness resolver (extends the existing per-turn `stalenessNotice` — it is the same "this changed, re-check it" signal pointed at a referenced section instead of a whole file).
5. `TEST-{id}.md` is generated from approved artefacts only, never from memory — an un-run stage yields an empty section; the agent never fabricates.

**`TEST-{id}.md` schema (six sourced sections):**
```
Functional      — ACs (AC-{req_id}-{seq}) + FLOW-{id}.md paths     (P01 + P04e)
Non-functional  — REQ NFRs + ARCH.md + DESIGN.md + API-CONTRACT     (P01 + P03 + P04)
Clinical safety — HAZARD-REGISTRY.md HAZ-IDs                        (P06)
Security        — SECURITY-REGISTRY.md controls                    (P08)
IG              — IG.md / DPIA.md controls                         (P07)
Evaluation      — Evaluation Function Specification CHECKs          (P01)
```
Every test references its source (AC/HAZ/SEC/IG/CHECK/flow-path). A test with no traceable source is a Review-Agent / P09 failure — it means invented behaviour.

**Change management is baked in — no new mechanism.** `propose_requirement_change` (Plan 3d) + the per-turn `stalenessNotice`, extended by decision 4's section-scoped resolver. A requirement change marks the referencing sections + `TEST-{id}.md` stale; Agent A re-drafts; the `TASK-NNN-CODE.can_start` gate holds Agent B until the new tests are human-approved.

**P09 Normalisation** shifts from section-presence checks to reference-integrity checks: every REQ reference resolves; every project-artefact section carries its back-references.

**Mixed structure needs no migration.** New projects use the new structure from day one. Existing projects keep their current REQ files; the pipeline reads both shapes — a stage looks for its content whether it sits as a section inside the REQ or as a reference to a dedicated artefact, and resolves it either way. Old projects age out naturally as the migration programme completes them. No big-bang rewrite.

**Depends on:** AC stable IDs (Plan 4e Phase 1). New build in Plan 5: per-stage prompt changes (summary + reference out, back-reference in), the requirement-reference staleness resolver, the `TEST-{id}.md` schema + Agent A generation, and the P09 rewrite.

**Deferred (named):** concurrent-write contention on project-level files is a Plan 6 (swarm) concern, not a requirements-pipeline concern — the pipeline is sequential and human-gated, so two agents never write `ARCH.md` at once. Deferred to be informed by production evidence, not pre-engineered.

---

## Plans 6 + 7 — Code Swarm 📋 PENDING

**Prerequisites:** Plan 5, Knowledge Graph Service Phase 1-4.

**Language:** C# for all domain logic. LangGraph (Python, self-hosted) for orchestration shell only — calling into C# API for all domain work.

**Plan 6 — Swarm Planning:** Task decomposition using the knowledge graph. Wave sequencing. LangGraph orchestration shell. Human-in-the-loop interrupt/resume. Domain correctness flagging (CLIN-/IG- → CSO/DPO sign-off gate).

**Plan 7 — Manifest Generator + Executor:** TASK-NNN.json generation from approved REQ files. MANIFEST-INDEX.json. Copy Agent Prompt generation. Delivery schedule UI.

---

## Plan 8 — .genesis/ Repo Sustainability 📋 PENDING (after Plan 6)

Long-running project health. .genesis/ skill files evolve with the project. Review gates prevent skill drift. Archival policy for completed manifests.

---

## Plan 9 — Managed Learning Loop 📋 PENDING (after Plan 6)

Six signal types collected from Postgres and GitHub after each wave. LLM generates specific, actionable improvement proposals for skill files and prompt sections. Human approves via Improvement Proposals tab. Approved changes auto-committed to .genesis/ in GitHub.

Signal types: guardrail fire rate, park rate, steer frequency, human fix commits, test failure rate by REQ category, PR review rejection rate.

Key decisions locked: Clinical/IG/Security proposals require named sign-off before approval. Proposal history never deleted. Signal thresholds set from real data — run 3-5 waves before enabling proposals.

---

## Plans 10–12 — Fine-tuning → Autonomous 📋 PENDING

- Plan 10: PEFT/LoRA fine-tuning of coding model on EMIS-specific patterns (after Plan 6)
- Plan 11: Fine-tuning of clinical safety and IG validation model on DCB0129/0160 decision history (after Plan 10)
- Plan 12: Full autonomous delivery cycle. Human approval gates preserved at regulated decision points (after Plan 11)

---

## Plan 14 — Cross-Req Coupling ⏸️ DEFERRED

Implement only when one of these fires in production:
- Duplicate HAZ-ID assigned across two requirements
- Clinical safety control in REQ-N contradicts decision in earlier REQ
- P06/P07/P08 reviewer finds cross-req coupling the cross-check missed

Until then: deferred.

---

## Pipeline Stages

```
P01 — Requirements Discovery
P02 — Prototype Demo Builder
P03 — Architecture
P04 — Design (API/DB)
P05 — PxD
P06 — Clinical Safety (DCB0129)
P07 — Information Governance / DPIA
P08 — Security
P09 — Medical Device (MDR/MHRA) — PLANNED, design session with Indra required first
P10 — Pre-Swarm Decision Gate
P11 — TDD / Code Generation
```

P09 position and conditional vs always-on status to be confirmed with Indra before any engineering begins.

Flow Spec (Plan 4e) is an optional per-requirement artefact produced in P01 and projected into P02, P06 and the TDD agent — it is not a pipeline stage.

---

## Artefact Structure in Feature Repos

```
{feature-repo}/
  .genesis/
    requirements/         REQ-{id}.md, CHANGE-{id}.md, FLOW-{id}.md, TEST-{id}.md
    architecture/         ARCH.md
    design/               DESIGN.md, API-CONTRACT.yaml, DB-SCHEMA.sql,
                          DATA-MODELS.md, ERROR-CATALOGUE.md, CONTRACT.md
    pxd/                  PXD.md
    clinical-safety/      HAZARD-REGISTRY.md, HAZARD-REGISTRY.xlsx
    ig/                   IG.md, DPIA.md
    security/             SECURITY-REGISTRY.md
    prototype/            index.html
    session-close/        SESSION-CLOSE-P01.md … SESSION-CLOSE-P08.md
    review/               REVIEW-{id}.md
    project/              PROJECT.md
```

**Scope:** the only per-requirement files (carrying a `{id}` suffix) are `REQ-{id}.md`, `FLOW-{id}.md`, `CHANGE-{id}.md` and `TEST-{id}.md` — what an agent loads for a single task. Every other artefact is per-project: one file, ratified once by the owning role, referenced by many requirements.

**The REQ file is a thin index.** It holds P01-owned content only (requirements, ACs with stable `AC-{req_id}-{seq}` IDs, the Evaluation Function Specification, compliance anchors) plus a References block pointing into each downstream artefact. Downstream stages write their full output to their own artefact and only a summary + reference into the REQ. See `artefact-scope-restructure-design.md`.

`FLOW-{id}.md` is the human-readable Mermaid rendering of a requirement's flow, for audit only. The structured flow model lives in the DB and is what downstream stages read — the rendering is never parsed.

**Traceability is bidirectional:** the REQ references the project artefact; each project artefact section references back the `REQ-{id}`s it serves. P09 Normalisation enforces reference integrity in both directions.

**Mixed structure needs no migration.** New projects use this structure; existing projects keep their current REQ files. The pipeline reads both shapes and resolves a stage's content whether it sits inline in the REQ or as a reference to a dedicated artefact. Old projects age out naturally.

*Changes vs v4.2:* `architecture/ARCH-{id}.md` → `architecture/ARCH.md`; `ig/IG-{id}.md` → `ig/IG.md` (+ `DPIA.md`); `security/SEC-{id}.md` → `security/SECURITY-REGISTRY.md`; new `pxd/PXD.md`; new `requirements/TEST-{id}.md`; `clinical-safety/DCB0129-{id}.*` → `clinical-safety/HAZARD-REGISTRY.*`.

---

## Feature Flags — Complete Registry

```
TokenOptimisation.FoundationPrefixEnabled         ✅ LIVE
TokenOptimisation.RequirementWindowingEnabled     ✅ LIVE
TokenOptimisation.NonWindowedCrossCheckEnabled    ✅ LIVE
TokenOptimisation.PrototypeDomModeEnabled         ✅ LIVE
TokenOptimisation.PrototypeFragmentsEnabled       ✅ LIVE (legacy — active until Plan 4 prod flag flip)
TokenOptimisation.ActiveSkillInjectionEnabled     ✅ LIVE
TokenOptimisation.RequirementFeedbackEnabled      ✅ LIVE
TokenOptimisation.PrototypeSingleFileEnabled      ✅ LIVE in Dev / ❌ false in Production — flip after anchor fix

FlowSpec.Enabled                                 📋 Plan 4e
FlowSpec.DownstreamInjectionEnabled              📋 Plan 4e (gates P02/P06/TDD projection separately)

KnowledgeService.Enabled                         ✅ LIVE — Plan 4b
KnowledgeService.ProjectArtefactIndexingEnabled  ✅ LIVE — Plan 4b
HelpChat.Enabled                                 ✅ LIVE — Plan 4b

KnowledgeGraph.IndexerEnabled                    📋 KG Phase 1
KnowledgeGraph.McpServerEnabled                  📋 KG Phase 1
KnowledgeGraph.PipelineInjectionEnabled          📋 KG Phase 4
KnowledgeGraph.BlastRadiusEnabled                📋 KG Phase 5
KnowledgeGraph.LegacyIndexingEnabled             📋 KG Phase 5

Project.GitHubIntegrationEnabled                 ✅ LIVE — Plan 4c
Project.ArtefactPushEnabled                      ✅ LIVE — Plan 4c
Project.SessionCloseEnabled                      ✅ LIVE — Plan 4c

Swarm.ManifestGenerationEnabled                  📋 Plan 6
Swarm.GitHubWebhookEnabled                       📋 Plan 6
Swarm.SeparatedTddEnabled                        📋 Plan 5

ArtefactRestructure.Enabled                      📋 Plan 5 (REQ-as-index + per-project artefacts)
ArtefactRestructure.TestRegistryEnabled          📋 Plan 5 (TEST-{id}.md generation)

Learning.SignalCollectionEnabled                 📋 Plan 9
Learning.ProposalGenerationEnabled               📋 Plan 9
```

---

## Language Decision Map

Everything in Genesis AI = C# (.NET 10), with one deliberate exception: Plan 6's orchestration shell (LangGraph, Python, calling into C# API for all domain work).

The only non-C# artefacts:
- Prompt files (.md) — pipeline prompts, skills, guardrails
- SQL migrations — Flyway .sql files
- .genesis/ skill files committed to GitHub — Markdown
- Plan 6 orchestration shell — LangGraph (Python)
- GitHub Actions YAML — triggers C# indexer binary

---

## Implementation Order

```
COMPLETE:
  Plan 4b — Knowledge Service + Help Chat Panel ✅
  Plan 4c — GitHub Integration + Platform Extensions ✅ (PRs awaiting merge)

NOW (active):
  Plan 4d — Engineering Foundation 🔄
             Contract layer implementation (create command next)
             Anchor failure root cause investigation
             Engineering hygiene items
             Pipeline Prompt Quality Review (P01–P11)
  Plan 4d-R — PR Review Agent (design complete, wiring pending)
  Plan 4e — Flow Spec (in design — P01→P02 behavioural flow artefact)

PARALLEL (Darren's team):
  Knowledge Graph Service Phase 1-2

AFTER Plan 4 production flag flip (gated only on the flag, NOT on the full 4d→5 gate):
  Plan 4e build — Phase 1 (flow model + validator + draft_flow),
                  Phase 2 (render + audit/approve loop).
                  Runs in parallel with the rest of Plan 4d and with Plan 5.
                  Downstream projections land incrementally: P02 and P06 first,
                  the TDD path-to-test projection with Plan 5.
                  Enriches Plan 5 — does NOT gate it.

AFTER 4d complete (hard gate — all Plan 4d items checked off):
  Plan 5  — Two-Agent TDD + Code Quality Skills
  KG Phase 3-4 (parallel with Plan 5)

AFTER 5 + KG Phase 1-4:
  Plan 6  — Swarm Planning
  Plan 7  — Manifest Generator (alongside Plan 6)
  KG Phase 5 (ongoing enrichment)

AFTER 6:
  Plan 8  — .genesis/ Sustainability
  Plan 9  — Learning Loop
  Plan 10 — Fine-tuning: Coding Model
  Plan 11 — Fine-tuning: Validation Model
  Plan 12 — Autonomous Loop
```

---

### PR size rule
If a PR touches more than 20 files or takes more than a day to implement — it is too big. Split it at the deliverable boundary.

### Cherry-pick discipline
Cherry-pick from exp to PR branch **frequently and in real time** — not at the end of a plan when 136 commits have accumulated. Each time a logical piece is solid on exp, cherry-pick it across. The PR branch always has a readable history; the exp branch is disposable.

### What went wrong (and why the rule exists)
Plans 4b, 4c, and 4d were built on a single exp branch over months. PRs were raised directly from exp. When the PRs merged and new work accumulated, the exp branch and main diverged in a way that made cherry-picking impossible (intermediate stub commits conflicted with final state already on main). The result was a single 266-file commit on the PR branch — unreviewed in any meaningful sense.

The fix is structural: PR branches are created before work starts, not after.


---

## The Right Balance

Your system achieves:
- 10x engineering throughput on well-scoped tasks
- Full DCB0129/DCB0160 compliance at every delivery
- Complete audit trail from requirement to test to code in git
- Zero circular validation — tests written before code, by a separate agent
- Managed self-improvement — the system gets better every cycle
- Human accountability preserved at every regulated decision point
- Competitive moat — the knowledge graph that compounds with every feature shipped

The constraints are the competitive advantage.
