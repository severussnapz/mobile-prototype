# Genesis AI Requirements API — Platform Guide

## What the Platform Does

Genesis AI orchestrates an end-to-end requirements lifecycle where each pipeline stage is powered by an AI interviewer (Claude Sonnet via AWS Bedrock) that asks structured questions, captures requirements as artefacts, and manages cross-cutting concerns in a parking lot.

The platform guides product teams through a structured requirements pipeline — from discovery through clinical safety, information governance, security, and into code generation. Every stage produces a defined artefact. Every artefact is approved by a human. Every approved artefact is stored in S3, committed to Git, and indexed into the knowledge base.

---

## Pipeline Stages

| Stage | Name | Purpose |
|-------|------|---------|
| P01 | Requirements Discovery | AI-driven interview capturing product scope, personas, constraints |
| P02 | Prototype Demo Builder | Generates clickable single-file HTML prototypes to validate requirements |
| P03 | Architecture | Technical architecture decisions (ADRs, BDAT, failure modes) |
| P04 | Design | Implementation design (OpenAPI contracts, DDL schemas, interfaces) |
| P05 | PxD | Product and UX design (user flows, wireframes, accessibility) |
| P06 | Clinical Safety | DCB0129/0160 compliance (hazard log, guardrail mapping) — clinical domains only |
| P07 | Information Governance | DPIA, data flows, lawful basis, records of processing |
| P08 | Security | Threat modelling, security review workbook, control mapping |
| P09 | Medical Device | MDR/MHRA compliance — planned, design session with Indra required first |
| P10 | Pre-Swarm Decision Gate | Consolidated review of all approved artefacts before code generation |
| P11 | TDD / Code Generation | Test suite generation from CHECKs + production code by AI swarm |

---

## How the AI Tool Loop Works

When a user sends a message, the API streams the AI response via SSE while the LLM invokes tools to produce side effects:

1. User sends message → controller builds AI context (system prompt + prior messages + prior stage artefacts)
2. AI streams text chunks → `data: {"text": "..."}` events sent immediately to frontend
3. AI invokes tool(s) → controller executes each tool, persists result to DB, sends SSE event
4. Tool results sent back to AI as a continuation message
5. AI produces more text/tools → loop continues (up to 40 turns safety limit)
6. AI finishes → `data: [DONE]` sent, full response stored as message

---

## Available AI Tools

| Tool | Purpose |
|------|---------|
| `save_artefact` | Save a file (manifest.md, REQ-*.md, etc.) — uploads to S3, stores metadata in DB |
| `get_artefact` | Read a specific file's content — read-only |
| `list_artefacts` | Discover what files exist in the project — read-only |
| `edit_artefact` | Make a surgical edit by replacing an exact anchor string — blocked until search_in_artefact has run |
| `search_in_artefact` | Search for lines containing a query string (returns ±5 lines context) — must run before edit_artefact |
| `advance_phase` | Move to next interview phase |
| `advance_requirement` | Move to the next requirement window (per-requirement processing) |
| `update_progress` | Report questions asked / estimated total |
| `add_parking_lot_item` | Defer a topic for later |
| `resolve_parking_lot_item` | Mark a parking lot item as resolved |
| `set_orchestration_mode` | Switch between forward_sweep and cross_check modes (P06–P08) |
| `get_guardrail_details` | Load a skill/guardrail document — read-only |

---

## Data Model

```
Project (aggregate root)
├── PipelineStage × 11 (P01–P11, auto-created)
│   └── Conversations (one or more per stage)
│       ├── Messages (user + assistant turns)
│       ├── ParkingLotItems (stored per-conversation)
│       └── TokenUsageRecords (input, output, cache tokens)
├── Artefacts (stored at project level — cross-stage visibility)
├── HelpConversation (help chat — separate from pipeline, no StageId)
│   └── HelpMessage
├── KnowledgeDocument (pgvector — genesis-tool + project-artefact namespaces)
├── Notes (free-text notes with author metadata)
└── Decisions (ADR-style decisions)
```

---

## Key Design: Artefacts Are Project-Scoped

Artefacts belong to the project, not a specific conversation or stage. This means:

- Later stages can read artefacts from earlier stages (Architecture reads Requirements Discovery output)
- The AI uses `list_artefacts` and `get_artefact` tools to pull prior work as context
- Saving the same `file_path` again creates a new version (latest wins, previous versions preserved in S3)
- On approval: the artefact is indexed into pgvector (`project-artefact` namespace) via `ArtefactPublishedDomainEvent`

---

## Key Design: S3 Content Storage

Artefact content lives in S3. The database stores only metadata.

| Concern | Where stored |
|---------|-------------|
| file_path, version, s3_key, content_type, size_bytes, created_by | PostgreSQL artefacts table |
| Actual file content (markdown, HTML, JSON, etc.) | S3 bucket `genesis-ai-artefacts` |

Storage key scheme: `projects/{projectId}/artefacts/{filePath}/v{version}`

---

## Key Design: Knowledge Service (Plan 4b)

Two knowledge namespaces in pgvector:

**`genesis-tool`** — Genesis AI pipeline documentation. Seeded on deployment from embedded markdown resources (Prompts/, Skills/, KnowledgeBase/ folders). Answers questions about how the tool works, what each stage does, how to write good prompts, what GAP/CLARIFICATION/CONTRADICTION mean.

**`project-artefact`** — Approved artefacts per project. Indexed at artefact approval time via `ArtefactPublishedDomainEventHandler`. Tagged by `projectId`. Answers questions about a specific project's requirements, decisions, hazards, and architecture.

The help chat panel queries both namespaces on every turn. Project artefacts are prioritised when a `projectId` is present.

---

## Key Design: Parking Lot Is Project-Wide

Parking lot items are stored on individual conversations but queried at project level. Items raised in Requirements Discovery can be resolved in Architecture or any later stage. Priority levels: critical / high / medium / low. Status: open / resolved / deferred.

---

## Key Design: Session Continuity

Every pipeline stage maintains session continuity across browser sessions and multi-day work:

- **Conversation persistence** — full chat history stored in DB, restored on return
- **Artefact persistence** — every approved artefact in S3, indexed in pgvector, committed to `.genesis/` in feature repo (Plan 4c)
- **SESSION-CLOSE artefact** — generated when user closes a session. Summarises what was captured, what is open, and where to pick up next time. Injected at top of context on next session start.
- **ContinuedFromConversationId** — when a session hits the tool-use limit, a new conversation is created linked to the previous one. Handover context injected automatically.

---

## Key Design: Staleness Detection

On every message, the controller compares the latest artefact timestamp against the last message timestamp in the conversation. If artefacts are newer, a staleness warning is injected into the system prompt:

> ⚠️ ARTEFACTS UPDATED: Project artefacts have been modified since your last message. Use list_artefacts and get_artefact to reload any files you previously referenced — they may have changed.

This ensures the LLM never works with stale data when stages are revisited out of order.

---

## API Endpoints — Key Ones

| Method | Path | Purpose |
|--------|------|---------|
| POST | /api/v1/projects | Create project (auto-creates pipeline stages) |
| GET | /api/v1/projects/{id} | Get project with stages |
| GET | /api/v1/projects/{id}/artefacts | List all project artefacts |
| GET | /api/v1/projects/{id}/artefacts/{artefactId} | Get artefact with content |
| POST | /api/v1/conversations/{id}/stream | Send message — SSE streaming |
| POST | /api/v1/conversations/{id}/session-close | Generate SESSION-CLOSE artefact |
| GET | /api/v1/projects/{id}/parking-lot | Aggregate parking lot (all conversations) |
| GET | /api/v1/projects/{id}/token-usage | Token usage + cost per stage |
| GET | /api/v1/projects/{id}/export | Download ZIP of all artefacts |
| GET | /api/v1/help/conversations | Get help conversation for user + project |
| POST | /api/v1/help/stream | Stream help chat response (SSE) |
| POST | /api/v1/projects/{id}/hazard-log | Generate IF678 hazard log (.xlsx) |
| POST | /api/v1/projects/{id}/security-review-report | Generate security review report (.xlsx) |
| POST | /api/v1/projects/{id}/data-protection-impact-assessment | Generate DPIA report (.docx) |

---

## Key Design Decisions

**Native PostgreSQL enums** — type safety at the DB level, never text columns. Registered at three C# levels: Npgsql driver, EF Core SQL generation, EF Core model metadata.

**CQRS without event sourcing** — MediatR for separation, EF Core for persistence.

**Embedded prompts and skills** — system prompts versioned with code as embedded resources in `Genesis.AI.Infrastructure/Prompts/` and `Genesis.AI.Infrastructure/Skills/`.

**SSE streaming with tool loop** — real-time delivery without WebSocket complexity.

**Soft-delete** — projects are never physically deleted. `IsDeleted` flag on domain entities.

**Explicit entity configurations** — `GenesisAiDbContext` does NOT use `UseSnakeCaseNamingConvention`. Every entity requires an explicit `IEntityTypeConfiguration<T>` with `ToTable()` and `HasColumnName()` for every property matching the Flyway migration exactly.

**Domain events for side effects** — `ArtefactPublishedDomainEvent` fires on artefact approval, triggering pgvector indexing and (from Plan 4c) GitHub artefact push. Best-effort — never blocks approval.

**Prompt caching** — cache checkpoint after system prompt. ~90% cost reduction on repeated context in tool loops.

**Per-turn token tracking** — every Bedrock response records input, output, cache read, and cache write tokens with cost estimation.

**Foundation prompt prefix** — stable upstream artefacts loaded in full before the Bedrock cache point (~10x cheaper cached tokens).

**Per-requirement windowing** — conversations can be scoped to a single requirement, giving each a bounded message window. AI moves between requirements with `advance_requirement` tool.

**Explicit orchestration modes** — `forward_sweep` (default, windowed) and `cross_check` (non-windowed holistic pass for P06–P08). Switched via `set_orchestration_mode`, never inferred.

---

## Authentication and Authorisation

JWT Bearer with scope-based policies. Every controller action requires `[Authorize(Policy = "...")]`.

| Scope | Purpose |
|-------|---------|
| genai-req.admin | Full access |
| genai-req.read | Read projects, conversations, artefacts |
| genai-req.write | Create/modify projects, conversations |
| genai-req.arch | Converse on Architecture stage |
| genai-req.pxd | Converse on Product Design stage |
| genai-req.clin | Converse on Clinical Safety stage |

---

## Database Migrations

Flyway format: `V{version}__description.sql` in `db/migrations/`.

Current migrations: V1 (initial schema) through V20 (help_conversation + help_message tables).
V21 and V22 reserved for Plan 4c (GitHub config + P00 fields).

All new tables require a Flyway migration. UUID primary keys use `uuid_generate_v4()`. Timestamps use `TIMESTAMPTZ`. Table names are singular snake_case.

---

*Genesis AI Requirements API Platform Guide v1.0 | July 2026*
*Next update: when Plan 4c (GitHub integration) lands*
