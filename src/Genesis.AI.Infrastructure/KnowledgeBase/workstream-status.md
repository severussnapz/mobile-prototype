# Genesis AI — Workstream Status

Last updated: June 2026

---

## A — Genesis AI Core
**Status: Live — Plans 1 through 3e complete**
The core pipeline is operational. Product teams can use it today.

| Capability | Status |
|---|---|
| Requirements pipeline (P01) | ✅ Live |
| Prototype generation (P02) | ✅ Live |
| Architecture pipeline (P03) | ✅ Live |
| Design pipeline (P04) | ✅ Live |
| PxD pipeline (P05) | ✅ Live |
| Clinical safety (P06 — DCB0129) | ✅ Live |
| IG pipeline (P07) | ✅ Live |
| Security pipeline (P08) | ✅ Live |
| Token refactor + foundation prefix caching | ✅ Live — Plan 1 |
| Skill decomposition + phase-aware injection | ✅ Live — Plan 2 |
| Fragment-based prototype generation | ✅ Live — Plan 3 |
| Prototype editing hardening (unique DOM IDs) | ✅ Live — Plan 3a |
| Prototype DOM migration (AngleSharp) | ✅ Live — Plan 3b |
| apply_to_scope (generic bulk DOM editing) | ✅ Live — Plan 3c |
| swap_class atomic operation | ✅ Live — Plan 3e |
| Requirement change feedback loop (GAP/CLARIFICATION/CONTRADICTION) | ✅ Live — Plan 3d |
| Domain impact classification (CS/IG/SEC badges) | ✅ Live — Plan 3d |
| AC insertion into REQ files on approval | ✅ Live — Plan 3d |
| Prototype fragment migration service (monolith → fragments) | ✅ Live — Plan 3e |
| Prototype skills (state detection, edit discipline, build discipline) | ✅ Live — Plan 3e |
| Pipeline02 cleanup (skills extracted, legacy rules removed) | ✅ Live — Plan 3e |
| Weekly reporting system | ✅ Live |
| apply_bulk_attributes | ✅ Superseded by apply_to_scope |

**Open issue:** Assembly pipeline produces broken HTML when `_shell.html` was migrated without GENESIS markers. `InjectMetadataIntoShellAsync` needs marker injection. Fix required before Plan 3c/3d PRs are raised.

---

## B — Pipeline Edit Reliability
**Status: Substantially complete — assembly fix pending**
The ability to make precise, immutable, auditable changes to any pipeline output at any stage.

| Capability | Status |
|---|---|
| Text pipeline edits (REQ, ARCH, DCB0129, IG) | ✅ Working |
| Prototype DOM edits (single element, set_node_attribute) | ✅ Working |
| Prototype bulk edits (apply_to_scope) | ✅ Live — Plan 3c |
| Atomic class swap (swap_class) | ✅ Live — Plan 3e |
| Fragment migration (monolith → editable fragments) | ✅ Live — Plan 3e |
| Assembly pipeline (fragments → index.html) | 🔧 Fix pending — GENESIS markers missing from InjectMetadataIntoShellAsync |
| Template contracts (schema per pipeline) | ✅ Live — Plan 3d |
| Immutable versioning (S3) | ✅ Working |
| Audit trail (CHANGE-{id}.md) | ✅ Live — Plan 3d |
| Feedback loop classification | ✅ Live — Plan 3d |

---

## C — Context Graph
**Status: Design complete — build starting (parallel track)**
The EMIS Knowledge Graph Service: a central, always-on service that indexes all EMIS/Optum UK code repositories and exposes the graph via MCP server. Every Genesis AI pipeline, Copilot, and Cursor instance connects to it.

Owner: Darren Sheavills (AI/Architecture/Bids domain)
Repo: `emis-knowledge-graph` (new — pending creation)

**Three graphs (combined = the moat):**

| Graph | Sources | Status |
|---|---|---|
| Requirements graph | Genesis AI artefacts (REQ files, hazard logs, ACs, CHANGE records) | 📋 Build Phase 1 |
| Codebase graph | All repos — Roslyn (C#), ts-morph (TypeScript), Python ast, VB6 structural, SQL parser | 📋 Build Phase 1 |
| Infrastructure graph | Terraform state, AWS Config, K8s manifests, GitHub Actions | 📋 Build Phase 3 |

**Seed (25 years of EMIS history):**

| Seed source | Status |
|---|---|
| Git history (all repos — 25 years of commits) | 📋 Phase 5 |
| ServiceNow (incidents, change requests, problem records) | 📋 Phase 5 |
| Confluence (ADRs, runbooks, post-mortems, design docs) | 📋 Phase 5 |
| DCB0129/DCB0160 artefacts (every hazard log ever filed) | 📋 Phase 5 |
| NHS contracts and standards (GP SoC, ISB, NHS Digital APIs) | 📋 Phase 5 |
| Support tickets (25 years, 35M patients, 59% of GP practices) | 📋 Phase 5 |

**MCP server tools:**

| Tool | Status |
|---|---|
| graph_search_entities | 📋 Build Phase 2 |
| graph_get_neighbours (centrality-weighted traversal) | 📋 Build Phase 2 |
| graph_get_schema (existing DB tables/columns) | 📋 Build Phase 2 |
| graph_get_endpoints (existing API routes) | 📋 Build Phase 2 |
| graph_get_blast_radius (betweenness-weighted impact) | 📋 Build Phase 3 |
| graph_get_patterns (established codebase patterns) | 📋 Build Phase 3 |
| graph_get_hotspots (change frequency + centrality) | 📋 Build Phase 3 |
| graph_get_test_coverage (coverage gaps) | 📋 Build Phase 3 |
| graph_get_migration_status (strangler fig progress) | 📋 Build Phase 3 |

**Pipeline acceleration (after graph active):**

| Pipeline | Before graph | After graph |
|---|---|---|
| P01 Requirements | 12 weeks | 6 weeks |
| P02 Prototype | 3 rounds | 1 round |
| P03 Architecture | 2 weeks | 3 days |
| P04 Design (API/DB) | 1 week | 1 day |
| P05 PxD | 1 week | 2 days |
| P06 Clinical Safety | 4 weeks | 1 week |
| P07 IG/DPIA | 3 weeks | 3 days |
| P08 Security | 2 weeks | 2 days |

**Technology:** PostgreSQL (existing estate, zero new infrastructure), C# throughout. No external graph platforms (TrustGraph, Neo4j evaluated and rejected — PostgreSQL with C# centrality computation is sufficient).

---

## D — TDD Agent
**Status: Not started — Plan 5**
Generates a full test suite from all approved pipeline documents. Two-agent model: Agent A writes tests from REQ CHECKs (cannot see implementation), Agent B makes them pass (cannot modify tests).

| Capability | Status |
|---|---|
| response-discipline skill (eliminates agent filler) | 📋 Plan 5 |
| efficient-implementation skill | 📋 Plan 5 |
| debugging-discipline skill (hypothesis-first) | 📋 Plan 5 |
| Two-agent TDD manifest structure (TASK-NNN-TESTS, TASK-NNN-CODE) | 📋 Plan 5 |
| TASK-NNN-CODE.can_start gate (blocked until TESTS human-approved) | 📋 Plan 5 |
| Behavioural tests from REQ acceptance criteria | 📋 Plan 5 |
| Integration tests from ARCH API contracts | 📋 Plan 5 |
| Safety tests from DCB0129 hazard log | 📋 Plan 5 |
| Compliance tests from IG controls | 📋 Plan 5 |
| Diagnostic task pipeline (bounded plan-challenge protocol) | 📋 Plan 5 |

**Dependency:** Needs Plan 4 (GitHub integration), Plan B complete

---

## E — Code Swarm
**Status: Not started — Plans 6 + 7**
AI swarm writes production code against pre-generated test suite. Task planning is graph-informed — high-centrality nodes identified before any code is written.

| Capability | Status |
|---|---|
| Swarm task decomposition (graph-weighted) | 📋 Plan 6 |
| Wave sequencing | 📋 Plan 6 |
| LangGraph orchestration shell | 📋 Plan 6 — Python (deliberate, permanent) |
| Human-in-the-loop interrupt/resume | 📋 Plan 6 |
| Domain correctness flagging (CLIN-*/IG-* → CSO/DPO gate) | 📋 Plan 6 |
| TASK-NNN.json generation | 📋 Plan 7 |
| MANIFEST-INDEX.json | 📋 Plan 7 |
| Copy Agent Prompt generation | 📋 Plan 7 |
| Delivery schedule UI | 📋 Plan 7 |
| .genesis/ scaffold committed by genesis-ai[bot] | 📋 Plan 4 + 7 |
| GitHub webhook processing | 📋 Plan 6 |
| Teams notifications | 📋 Plan 4 + 6 |
| PR generation + review workflow | 📋 Plan 6 |
| Architecture fitness checks (post-wave) | 📋 Plan 6 |
| Wave regression (CHECKs as integration tests) | 📋 Plan 6 |
| .genesis/ sustainability | 📋 Plan 8 |
| Managed learning loop | 📋 Plan 9 |
| End-to-end: conversation to deployed code | 📋 Plans 6-9 |

**Dependencies:** Plan 4 (GitHub integration + genesis-ai[bot]), Plan 5 (two-agent TDD), Knowledge Graph Service Phase 1-4

---

## F — AI Platform & Infrastructure
**Status: Partially resolved — key decisions made**

| Capability | Status |
|---|---|
| Frontier model selection | ✅ AWS Bedrock (Claude) — live |
| Air-gapped deployment (PrivateLink) | ✅ Architecture established |
| NHS data sovereignty compliance | ✅ Session data transient, no patient data in training loop |
| RAG vs graph vs vectorless SQL | ✅ Decided — graph + vectorless SQL, no RAG cross-contamination |
| Knowledge graph tooling | ✅ Decided — PostgreSQL + C# Roslyn, no external platform |
| MCP server (graph exposure to Copilot/Cursor) | 📋 Knowledge Graph Service Phase 2 |
| IDE integration (Copilot, Cursor) | 📋 Via MCP server |
| Genesis CLI | 📋 Not started |
| Fine-tuning: coding model (PEFT/LoRA) | 📋 Plan 10 — after Plan 6 |
| Fine-tuning: validation model (DCB0129/clinical) | 📋 Plan 11 — after Plan 10 |
| Hardware CapEx vs OpEx | ✅ OpEx — Bedrock on-demand |
| Guardrails-as-code | ✅ Live — 95/95 guardrails passing |
| Model usage tracking (tokens, cost per pipeline) | 📋 Not started |
| Specialist partner (LLM fine-tuning, clinical data) | 📋 TPG to provide introductions |

---

## G — Genesis Platform
**Status: In progress**
Assured, repeatable deployment infrastructure. The runway everything else takes off from.

| Capability | Status |
|---|---|
| Local development (LocalStack) | ✅ Working |
| Docker Compose dev environment | ✅ Working |
| PostgreSQL (local + prod) | ✅ Working |
| S3 artefact storage (LocalStack + AWS) | ✅ Working |
| Flyway migrations | ✅ Working |
| CI/CD pipeline | 🔄 In progress |
| AWS bootstrap (approved standard settings) | 📋 Not started |
| IaC (infrastructure as code) | 📋 Not started |
| Environment strategy (dev/staging/prod) | 📋 Not started |
| Observability baseline (logging, metrics, alerting) | 📋 Not started |
| Cost visibility per workstream | 📋 Not started |
| Model usage tracking (tokens, cost per pipeline) | 📋 Not started |

---

## Dependencies

```
A ──────────────────────────────────► always running — 8 pipelines live
B (assembly fix) ───────────────────► unblocks Plan 3c/3d PRs
C (Knowledge Graph) ────────────────► accelerates all 8 pipelines
                                      required by Plan 6 for graph-informed task planning
D (needs Plan 4 + B complete) ──────► unlocks E
E (needs C + D + Plan 4) ───────────► end-to-end delivery
F ──────────────────────────────────► key decisions made, fine-tuning deferred to Plans 10-11
G ──────────────────────────────────► foundation for all
```

---

## Immediate Priorities (This Week)

1. Fix assembly pipeline — GENESIS markers in `InjectMetadataIntoShellAsync`
2. Raise Plan 3c/3d PRs (API + App repos)
3. Start Knowledge Graph Service repo (Darren's team)
4. Start Plan 4 — Edit Project + Integrations (genesis-ai[bot] machine user setup first)
