# Genesis AI — Master Delivery Plan
Version: 4.0 — Updated July 2026
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
| 4b | Knowledge Service + Help Chat Panel | 📋 NEXT |
| 4c | GitHub Integration + Platform Extensions | 📋 AFTER 4b |
| KG | EMIS Knowledge Graph Service | 📋 PARALLEL TRACK — Darren's team |
| 5 | Code Quality: Skills + Two-Agent TDD | 📋 PENDING — after 4c |
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

**Status:** Complete (July 2026). Pending real-session BA validation and production flag flip.

**Architecture:**
- Generation: real `Conversation` linked to prototype `PipelineStage`. `PrototypeSingleFileEnabled` flag gates BOTH prompt selection AND tool selection atomically
- Surgical edits: right-click → `editElement` → `BedrockPrototypeDemoEditService` → `PrototypeElementReplacer` (fingerprint matching). Bypasses conversation tool loop intentionally — context fills up on large files
- Vibe edits: free-text → `sendMessage` → conversation AI → `edit_artefact` or `save_artefact`
- Token usage: recorded for both generation and surgical edits via `RecordSurgicalEditTokenUsageAsync`

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
- Test counts: 776 unit + 120 integration passing (API), 360 passing (App)

**Locked architectural decisions:**
- UI kit: EMIS-X only — no selector, no switching
- Edit architecture: model returns updated element HTML; PrototypeElementReplacer applies deterministically using fingerprint matching
- postMessage bridge: sends exact clicked element's outerHTML as context
- EMIS-X UI kit in stable Bedrock prompt cache — cached at 10x cheaper rate

**Pending before production:**
1. Real-session validation — a BA using the builder on an actual feature requirement
2. Flip `PrototypeSingleFileEnabled: true` in `appsettings.json`

**Wave H (after production flag flip):**
- Figma Option A — IFigmaImageService calls Figma `/v1/images/{file_key}`, returns PNG, feeds into existing vision input path. PAT stored as project-level secret

**Note on legacy fragment pipeline:** Plans 3b/3c/3e (apply_to_scope, swap_class, AngleSharp DOM pipeline) remain active in production until Plan 4 is proven in real sessions and the production flag is flipped. The fragment pipeline is not retired until Wave G cleanup is confirmed complete in production.

---

## Plan 4b — Knowledge Service + Help Chat Panel 📋 NEXT

**Status:** Next — no dependency on Plan 4 production flag flip. Starts immediately.

**What it is:** Two interconnected deliverables that give Genesis AI a living knowledge layer and a persistent help interface available from any pipeline stage.

### Genesis AI Knowledge Service

Inside `genesis-ai-requirements-api` — not a separate repo. Owned by the Genesis AI requirements team. When Workstream C (full Knowledge Graph) delivers, `IKnowledgeService` is swapped for a Knowledge Graph MCP client — one clean swap, no rearchitecting.

**Technology:** PostgreSQL with pgvector extension. Bedrock Titan Text Embeddings v2. Both already in the VPC.

**Two namespaces, one table:**
- `genesis-tool` — Genesis AI pipeline documentation (global). Seeded on deployment from embedded markdown resources. Updated via PR governed by CODEOWNERS
- `project-artefact` — approved artefacts per project. Indexed at artefact approval time tagged with projectId. Re-indexed on amendment

**IKnowledgeService interface:**
```csharp
Task IndexDocumentAsync(string @namespace, Guid? projectId, string sourcePath,
    string content, Dictionary<string, string> metadata, CancellationToken ct);

Task<IReadOnlyList<KnowledgeChunk>> QueryAsync(string query, string @namespace,
    Guid? projectId, int topN = 5, CancellationToken ct);

Task DeleteBySourcePathAsync(string @namespace, Guid? projectId,
    string sourcePath, CancellationToken ct);
```

**Workstream C plug-in point:** Replace `BedrockKnowledgeService` with `KnowledgeGraphMcpClient`. DI registration swap only. No changes to callers.

### Help Chat Panel

Not a pipeline stage. Not tied to PipelineStage. Not a new route.

**HelpConversation aggregate** — dedicated lightweight aggregate. No StageId. No phases. No parking lot. ProjectId? and UserErn only. DB-persisted.

**Query pattern per turn:**
1. Embed user message via Bedrock Titan
2. Query `genesis-tool` — top 5 chunks from Genesis AI documentation
3. If projectId present — query `project-artefact` filtered by projectId — top 5 chunks from approved artefacts
4. Inject both into system prompt
5. Bedrock responds grounded in tool docs and project context

**Frontend:** Persistent floating panel rendered at Shell level in Routes.tsx. Toggle button always visible bottom-right. 400px wide, full viewport height, slides in from right. Available from every page without navigation.

**Migration sequence:** V18 (pgvector), V19 (knowledge_documents), V20 (help_conversations + help_messages)

**Build order:**
- Day 1: pgvector + Knowledge Service (IKnowledgeService, BedrockKnowledgeService, KnowledgeSeederService)
- Day 2: Artefact approval hook (index on approval, re-index on amendment, delete on project deletion)
- Day 3: HelpConversation aggregate + HelpChatController + HelpChatStreamService
- Day 4: HelpChatPanel in the app + wire into Shell
- Day 5: User guide (written, indexed into genesis-tool namespace, downloadable from tool)

**Success criteria:**
- User can ask "what does P06 do?" from any pipeline stage and get an accurate grounded answer
- User in P06 can ask "what requirements did we capture in P01?" and get an answer from the actual approved REQ file — not a hallucination
- When context not found, help chat says so directly and does not hallucinate
- Workstream C plug-in requires no changes to HelpChatStreamService or HelpChatPanel

---

## Plan 4c — GitHub Integration + Platform Extensions 📋 AFTER 4b

**Status:** After Plan 4b — depends on artefact approval hook established in 4b.

**What it is:** genesis-ai[bot] commits approved artefacts to .genesis/ in feature repos. Every approval is a Git commit. Git history is the audit trail and Knowledge Graph data feed.

**Core deliverables:**
- `genesis-ai[bot]` machine user — organisational, fine-grained PAT, never a human engineer's token
- `PATCH /api/v1/projects/{id}` — stores GitHub token + repo URLs + Teams webhook encrypted. Auto-registers webhooks. Test Connection validates bot token identity
- `ScaffoldGenesisStructureAsync` — commits .genesis/ folder structure to feature repos
- `GitHubIntegrationService.PushArtefactAsync` — post-approval artefact push to .genesis/ in feature repo. All artefact types: MD, XLSX, HTML, SESSION-CLOSE files, PROJECT.md
- P00 project setup form extension — new fields (CSO, IG owner, security reviewer, release type, assurance, MD flag). DB migration. Generates PROJECT.md
- SESSION-CLOSE button — all pipeline stages. One SESSION-CLOSE-P0n.md per stage, upserted not duplicated. Shared skill, stage-parameterised
- P06 Excel export — DCB0129-{id}.xlsx at approval time using ClosedXML. Stored in S3, pushed to .genesis/clinical-safety/
- P06 DB API integration — post-approval webhook to CS team's hazard tracking DB
- CODEOWNERS file — team-based, not individual-based. `@emisgroup/clinical-safety-owners` for P06, `@emisgroup/ig-owners` for P07, `@emisgroup/security-owners` for P08. Maintained via GitHub org team membership — the file itself never changes when individuals change roles

**Commit convention:**
```
chore: approve REQ-001 — Unified Inbound Document Inbox
Approved-by: [user display name] ([user ERN])
Pipeline-stage: P01
Project: documents-manager-increment-1
```

**Migration sequence:** V21 (project_github_config), V22 (p00_fields_on_projects)

**Prerequisite for:** Plan 6 (swarm needs GitHub webhooks), Knowledge Graph Service (indexer needs GitHub API access, bot token for commit attribution)

---

## EMIS Knowledge Graph Service 📋 PARALLEL TRACK

**Owner:** Darren Sheavills (AI/Architecture domain)
**Repo:** `emis-knowledge-graph` (new standalone repo)
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

## Plan 5 — Code Quality: Skills + Two-Agent TDD 📋 PENDING

**Prerequisite:** Plan 4c complete.

**What it is:**
- response-discipline.md skill — eliminates preamble bloat
- efficient-implementation.md — anti-patterns checklist for coding agents
- debugging-discipline.md — hypothesis-first debugging protocol
- Two-agent TDD: Agent A writes tests from REQ CHECKs (cannot see implementation), Agent B makes them pass (cannot modify tests)
- TASK-NNN-TESTS and TASK-NNN-CODE as separate manifest entries
- TASK-NNN-CODE.can_start = false until TASK-NNN-TESTS is human_approved

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

---

## Artefact Structure in Feature Repos

```
{feature-repo}/
  .genesis/
    requirements/         REQ-{id}.md, CHANGE-{id}.md
    architecture/         ARCH-{id}.md
    clinical-safety/      DCB0129-{id}.md, DCB0129-{id}.xlsx
    ig/                   IG-{id}.md
    security/             SEC-{id}.md
    prototype/            index.html
    session-close/        SESSION-CLOSE-P01.md … SESSION-CLOSE-P08.md
    project/              PROJECT.md
```

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
TokenOptimisation.PrototypeSingleFileEnabled      ✅ LIVE in Dev / ❌ false in Production — flip after BA validation

KnowledgeService.Enabled                         📋 Plan 4b Day 1
KnowledgeService.ProjectArtefactIndexingEnabled  📋 Plan 4b Day 2
HelpChat.Enabled                                 📋 Plan 4b Day 3

KnowledgeGraph.IndexerEnabled                    📋 KG Phase 1
KnowledgeGraph.McpServerEnabled                  📋 KG Phase 1
KnowledgeGraph.PipelineInjectionEnabled          📋 KG Phase 4
KnowledgeGraph.BlastRadiusEnabled                📋 KG Phase 5
KnowledgeGraph.LegacyIndexingEnabled             📋 KG Phase 5

Project.GitHubIntegrationEnabled                 📋 Plan 4c
Project.ArtefactPushEnabled                      📋 Plan 4c
Project.SessionCloseEnabled                      📋 Plan 4c

Swarm.ManifestGenerationEnabled                  📋 Plan 6
Swarm.GitHubWebhookEnabled                       📋 Plan 6
Swarm.SeparatedTddEnabled                        📋 Plan 5

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
NOW (active):
  Plan 4b — Knowledge Service + Help Chat Panel

NEXT (after 4b):
  Plan 4c — GitHub Integration + Platform Extensions
  Plan 4  — Production flag flip (after BA validation, runs in parallel)

PARALLEL (Darren's team):
  Knowledge Graph Service Phase 1-2

AFTER 4c:
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
