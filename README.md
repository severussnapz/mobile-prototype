# Genesis AI Requirements API

Backend REST API for the **Genesis AI Requirements Platform** — an orchestration system that guides product teams through a structured 10-stage requirements pipeline using AI-driven interviews, artefact generation, and stage progression management.

---

## What It Does

The platform orchestrates an end-to-end requirements lifecycle where each stage is powered by an AI interviewer (Claude Sonnet 4.6 via AWS Bedrock) that asks structured questions, captures requirements as artefacts, and manages cross-cutting concerns in a parking lot.

### Pipeline Stages

| # | Stage | Purpose |
|---|-------|---------|
| 1 | Requirements Discovery | AI-driven interview capturing product scope, personas, constraints |
| 2 | Prototype | Generates clickable single-file HTML prototypes to validate requirements |
| 3 | Architecture | Technical architecture decisions (ADRs, BDAT, failure modes) |
| 4 | Design | Implementation design (OpenAPI contracts, DDL schemas, interfaces) |
| 5 | PxD | Product & UX design (user flows, wireframes, accessibility) |
| 6 | Clinical Safety | DCB0129/0160 compliance (hazard log, guardrail mapping) — clinical domains only |
| 7 | Information Governance | DPIA, data flows, lawful basis, records of processing |
| 8 | Security | Threat modelling, security review workbook, control mapping |
| 9 | Normalisation | Transform human-readable requirements into machine-readable JSON |
| 10 | Planning | Generate dependency-ordered task files for coding agents |

---

## How the AI Tool Loop Works

When a user sends a message, the API streams the AI response via SSE while the LLM can invoke tools to produce side effects:

```
┌─────────┐     POST /conversations/{id}/stream      ┌─────────────┐
│ Frontend │ ──────────────────────────────────────── │   API       │
│  (SSE)   │ ◄── data: {"text": "..."}               │  Controller │
└─────────┘ ◄── event: progress                      └──────┬──────┘
            ◄── event: artefact                              │
            ◄── event: parking_lot_item                      │
            ◄── data: [DONE]                                 │
                                                             ▼
                                                      ┌─────────────┐
                                                      │  Bedrock AI │
                                                      │ (Claude S4) │
                                                      └──────┬──────┘
                                                             │
                                              Tool calls ◄───┘
                                                             │
                        ┌────────────────────────────────────┼──────────────────────┐
                        │                    │               │           │           │
                        ▼                    ▼               ▼           ▼           ▼
                  save_artefact      advance_phase    update_progress  add_parking   list/get
                        │                    │               │         _lot_item     _artefact
                        ▼                    ▼               ▼           │           │
                  ┌──────────┐       ┌────────────┐   ┌──────────┐     ▼           ▼
                  │ Artefact │       │Conversation│   │Conversation│  ┌──────┐   ┌──────┐
                  │  Table   │       │   Phase    │   │  Progress  │  │Parking│  │Read  │
                  │ (project)│       │  Advance   │   │   Update   │  │Lot DB │  │Prior │
                  └──────────┘       └────────────┘   └──────────┘  └──────┘   │Output│
                                                                                └──────┘
```

### Tool Loop Detail

1. User sends message → controller builds AI context (system prompt + prior messages + prior stage artefacts)
2. AI streams text chunks → SSE `data: {"text": "..."}` events sent immediately to frontend
3. AI invokes tool(s) → controller executes each tool, persists result to DB, sends SSE event
4. Tool results are sent back to AI as a continuation message
5. AI produces more text/tools → loop continues (up to 40 turns safety limit)
6. AI finishes (no more tool calls) → `data: [DONE]` sent, full response stored as message

### Context Injection (Per Message)

Every message to the LLM includes a system prompt assembled from:

| Section | Source | Purpose |
|---------|--------|---------|
| Base prompt | `Infrastructure/Prompts/{stage}.md` | Interview structure, phase definitions |
| Project context | Project record | Name, code, description, compliance domain |
| Session state | Conversation record | Current phase, questions asked, parking lot items |
| Artefact manifest | Artefact table | File paths + versions (LLM uses tools to read content) |
| Staleness notice | Timestamp comparison | Warns LLM if artefacts changed since last message |

### Staleness Detection (Cross-Stage Awareness)

Stages can be revisited in any order. When a user returns to a stage after other stages have modified artefacts, the LLM needs to know its prior context may be outdated.

**How it works:**

1. On every message, the controller compares `latest artefact timestamp` against `last message timestamp` in this conversation
2. If artefacts are newer → a staleness warning is injected into the system prompt:
   > ⚠️ ARTEFACTS UPDATED: Project artefacts have been modified since your last message in this conversation. Use `list_artefacts` and `get_artefact` to reload any files you previously referenced — they may have changed.
3. The LLM sees the warning + the updated manifest, and uses `list_artefacts` / `get_artefact` tools to reload changed content
4. This ensures the LLM never works with stale data when stages are revisited out of order

**Example scenario:**
- User completes Requirements Discovery → artefacts saved (manifest.md, REQ-001.md, etc.)
- User moves to Architecture → reads requirements, saves ADRs
- User goes back to Requirements Discovery to add a new requirement
- Controller detects: architecture artefacts are newer than last requirements message
- LLM is warned, reloads relevant files, and incorporates changes before continuing

### Available AI Tools

| Tool | Purpose | Side Effect |
|------|---------|-------------|
| `save_artefact` | Save a file (manifest.md, REQ-*.md, etc.) | Uploads content to S3; stores metadata + key in DB |
| `advance_phase` | Move to next interview phase | Updates conversation phase tracking |
| `update_progress` | Report questions asked / estimated total | Updates conversation progress metrics |
| `add_parking_lot_item` | Defer a topic for later | Creates parking lot item on conversation |
| `resolve_parking_lot_item` | Mark a parking lot item as resolved | Updates item status to resolved |
| `list_artefacts` | Discover what files exist | Read-only (returns file list) |
| `get_artefact` | Read a specific file's content | Read-only (returns content) |
| `get_guardrail_details` | Load a skill/guardrail document | Read-only (returns skill content) |
| `advance_requirement` | Move to the next requirement window (per-requirement processing) | Completes the current requirement conversation and opens the next |
| `set_orchestration_mode` | Switch between `forward_sweep` and `cross_check` modes (P6–P8) | Updates the conversation's orchestration mode |

---

## Data Model: Project-Level Scoping

```
Project (aggregate root)
├── PipelineStage × 10 (auto-created)
│   └── Conversations (one or more per stage)
│       ├── Messages (user + assistant turns)
│       └── ParkingLotItems (stored per-conversation)
└── Artefacts (stored at project level, tagged by stage)
```

### Key Design: Artefacts Are Project-Scoped

Artefacts belong to the **project**, not a specific conversation or stage. This means:
- Later stages can read artefacts from earlier stages (e.g., Architecture reads Requirements Discovery output)
- The AI uses `list_artefacts` and `get_artefact` tools to pull prior work as context
- `GET /api/v1/projects/{id}/export` downloads ALL artefacts as a ZIP
- Versioning: saving the same `file_path` again creates a new version (latest wins)

### Key Design: S3 Content Storage

Artefact **content lives in S3** (LocalStack locally); the database stores only metadata.

| Concern | Where stored |
|---------|--------------|
| `file_path`, `version`, `s3_key`, `content_type`, `size_bytes`, `created_by` | PostgreSQL `artefacts` table |
| Actual file content (markdown, HTML, JSON, etc.) | S3 bucket `genesis-ai-artefacts` |

**Storage key scheme:** `projects/{projectId}/artefacts/{filePath}/v{version}`
- Leading slashes in `filePath` are stripped before building the key
- Example: project `03735ad1…`, file `requirements/REQ-001.md`, version 2 → `projects/03735ad1…/artefacts/requirements/REQ-001.md/v2`

**Interface:** `IArtefactStorageService` (in `Domain/Interfaces/`) with three methods:
- `SaveContentAsync(projectId, filePath, version, content, contentType, ct)` → returns storage key
- `GetContentAsync(storageKey, ct)` → returns content or `null` if not found
- `DeleteContentAsync(storageKey, ct)`

**Implementation:** `S3ArtefactStorageService` (in `Infrastructure/Services/`). Bucket name read from `S3:ArtefactBucketName` configuration — throws `InvalidOperationException` on startup if missing.

**LocalStack:** A LocalStack container runs alongside the API in Docker Compose and pre-creates the `genesis-ai-artefacts` bucket via `localstack/init-s3.sh`. Seed data uploads artefact content objects to LocalStack so `GET /api/v1/projects/{id}/artefacts/{artefactId}` returns real content locally.

**Integration tests:** `IArtefactStorageService` is replaced with an in-memory mock (backed by `ConcurrentDictionary`) in `TestWebApplicationFactory` so tests never hit real S3.

### Key Design: Parking Lot Is Project-Wide

Parking lot items are **stored** on individual conversations (FK relationship) but **queried** at project level:
- `POST /api/v1/conversations/{id}/parking-lot` — AI adds items during a session
- `GET /api/v1/projects/{id}/parking-lot` — Aggregates ALL items across ALL conversations in the project
- Items have priority (critical/high/medium/low) and status (open/resolved/deferred)
- This lets items raised in Requirements Discovery be resolved in Architecture or Design

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Runtime | .NET 10.0 (ASP.NET Core) |
| Database | PostgreSQL 17.4 |
| Migrations | Flyway 12.1 |
| ORM | Entity Framework Core + Npgsql |
| CQRS | MediatR |
| Mapping | AutoMapper |
| Validation | FluentValidation |
| AI | AWS Bedrock (Claude Sonnet 4.6) |
| Object storage | AWS S3 (LocalStack locally) |
| Auth | JWT Bearer (scope-based policies) |
| Observability | Serilog, health checks, Dynatrace |
| Containerisation | Docker (multi-stage builds) |

---

## Prerequisites

- Docker & Docker Compose
- .NET 10 SDK (for IDE support / running tests outside Docker)
- AWS credentials configured (for Bedrock AI — optional for local UI testing)
- `.env` file with:
  - `IDENTITY_URL` — JWT authority URL
  - `AUDIENCE` — JWT audience
  - `JFROG_USER` / `JFROG_TOKEN` — JFrog Artifactory credentials
  - `GIT_TOKEN` — GitHub PAT for private packages

---

## Quick Start

```bash
# Start all services (postgres → flyway → seed → api)
docker compose up -d --build

# Rebuild or restart only the API without re-running seed
docker compose up -d --build --no-deps api

# Verify
curl http://localhost:5000/health
# → {"status":"Healthy"}

# Swagger UI
open http://localhost:5000/swagger
```

### Docker Compose Services

| Service | Image | Port | Purpose |
|---------|-------|------|---------|
| `postgres` | postgres:17.4-alpine | 5432 | Database |
| `flyway` | flyway:12.1.0-alpine | — | Runs migrations |
| `localstack` | localstack/localstack | 4566 | S3 emulation; creates `genesis-ai-artefacts` bucket |
| `seed` | postgres:17.4-alpine | — | Inserts DB test data + uploads artefact content to LocalStack |
| `api` | Dockerfile.dev | 5000→8080 | API server |

Services start in dependency order: postgres (healthy) → flyway → seed → api.

### Useful Commands

```bash
# Rebuild API after code changes without re-running seed
docker compose up -d --build --no-deps api

# View API logs
docker compose logs -f api

# Connect to database
docker compose exec postgres psql -U postgres -d genesis_ai_requirements

# Stop everything (preserves data)
docker compose down

# Full reset (⚠️ DESTROYS ALL DATA — ask first)
docker compose down -v
```

---

## Project Structure

```
src/
├── Genesis.AI.Api/              # HTTP layer (feature slices, auth, middleware, shared Http envelopes)
│   ├── Features/                # Vertical slices — one folder per feature (controller + models + profile)
│   │   ├── Projects/
│   │   ├── Conversations/
│   │   ├── Artefacts/
│   │   ├── Stages/
│   │   └── Export/
│   └── Http/                    # Shared envelopes: ApiResponse<T>, ApiErrorResponse, ApiError
├── Genesis.AI.Core/             # Base types (Entity, IAggregateRoot, logging)
├── Genesis.AI.Domain/           # Business logic (aggregates, commands, queries, enums)
│   └── Interfaces/              # IArtefactStorageService (+ other contracts)
└── Genesis.AI.Infrastructure/   # Data access (EF Core, repositories, AI services)
    ├── Prompts/                 # Embedded system prompts per stage (.md)
    ├── Services/                # S3ArtefactStorageService, BedrockAiService, etc.
    └── Skills/                  # Embedded skill/guardrail content (.md)
tests/
├── Genesis.AI.Tests/            # Unit tests (xUnit v3 + Moq)
├── Genesis.AI.IntegrationTests/ # Integration tests (WebApplicationFactory + InMemory)
├── Genesis.AI.ApiTests/         # E2E tests (Refit, hits running API)
└── Genesis.AI.TestFramework/    # Shared utilities (MockTokenGenerator)
db/
├── migrations/                  # Flyway SQL (V1–V9)
├── seeds/                       # Per-project seed files (<project-code>.sql); all run on boot
localstack/
└── init-s3.sh                   # Creates genesis-ai-artefacts bucket on LocalStack startup
```

---

## Authentication & Authorisation

JWT Bearer with scope-based policies. Every controller action requires `[Authorize(Policy = "...")]`.

| Scope | Purpose |
|-------|---------|
| `genai-req.admin` | Full access |
| `genai-req.read` | Read projects, conversations, artefacts |
| `genai-req.write` | Create/modify projects, conversations |
| `genai-req.arch` | Converse on Architecture stage |
| `genai-req.pxd` | Converse on Product Design stage |
| `genai-req.clin` | Converse on Clinical Safety stage |

---

## API Endpoints

### Projects

| Method | Path | Purpose |
|--------|------|---------|
| POST | `/api/v1/projects` | Create project (auto-creates 10 stages) |
| GET | `/api/v1/projects` | List projects (optional `?status=` filter) |
| GET | `/api/v1/projects/{id}` | Get project with stages |
| DELETE | `/api/v1/projects/{id}` | Soft-delete |
| GET | `/api/v1/projects/{id}/parking-lot` | Aggregate parking lot (all conversations) |
| GET | `/api/v1/projects/{id}/token-usage` | Aggregated token usage + cost per stage |
| GET | `/api/v1/projects/{id}/export` | Download ZIP of all artefacts |

### Artefacts

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/api/v1/projects/{id}/artefacts` | List all project artefacts |
| GET | `/api/v1/projects/{id}/artefacts/{artefactId}` | Get artefact with content |
| POST | `/api/v1/projects/{id}/artefacts` | Save one or more artefacts |

### Conversations

| Method | Path | Purpose |
|--------|------|---------|
| POST | `/api/v1/conversations` | Create conversation for a stage |
| GET | `/api/v1/conversations/{id}` | Get conversation with messages |
| GET | `/api/v1/conversations/by-stage/{stageId}` | List conversations for a stage |
| POST | `/api/v1/conversations/{id}/stream` | Send message (SSE streaming) |

### Conversation State

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/{id}/progress` | Get phase progress |
| POST | `/{id}/advance-phase` | Advance to next phase |
| PATCH | `/{id}/phase` | Set specific phase |
| GET | `/{id}/parking-lot` | List parking lot items |
| POST | `/{id}/parking-lot` | Add parking lot item |
| POST | `/{id}/parking-lot/{itemId}/resolve` | Resolve item |
| POST | `/{id}/parking-lot/{itemId}/defer` | Defer item |
| DELETE | `/{id}/parking-lot/{itemId}` | Delete item |

### Pipeline Stages

| Method | Path | Purpose |
|--------|------|---------|
| POST | `/api/v1/stages/{stageId}/complete` | Mark stage complete |
| POST | `/api/v1/stages/{stageId}/skip` | Skip a stage |

---

## Database

### Migrations

Flyway format: `V{version}__description.sql` in `db/migrations/`.

- `V1__initial_schema.sql` — Initial schema (tables, enums, indexes)
- `V2__artefact_content_to_s3.sql` — Drops `content` column from `artefacts` table; adds `s3_key`, `content_type`, `size_bytes`
- `V3__project_code_unique_excludes_deleted.sql` — Partial unique index so soft-deleted project codes can be reused
- `V4__add_notes_and_decisions.sql` — Adds notes and decisions tables
- `V5__add_project_time_sheet_code.sql` — Adds timesheet code to projects
- `V6__add_ig_security_pipeline_stages.sql` — Adds Information Governance and Security stage types
- `V7__backfill_ig_security_pipeline_stages.sql` — Backfills the new stages onto existing projects
- `V8__add_requirement_id_to_conversation.sql` — Adds `requirement_id` column + index (per-requirement windowing)
- `V9__add_orchestration_mode_to_conversation.sql` — Adds `orchestration_mode` enum type + column (forward sweep / cross-check)

To add a new migration: create the next versioned file (e.g. `V10__description.sql`) and rebuild.

### Enum Handling

All enums use **native PostgreSQL enum types** (never text columns). Registered at three C# levels in `DependencyInjection.cs`:

1. `dataSourceBuilder.MapEnum<T>()` — Npgsql driver
2. `UseNpgsql(o => o.MapEnum<T>())` — EF Core SQL generation
3. `modelBuilder.HasPostgresEnum<T>()` — EF Core model metadata

### Seed Data

Each file in db/seeds/ (named <project-code>.sql) creates a test project with conversations, messages, parking lot items, token usage records, and artefacts. The seed service runs every db/seeds/*.sql file on `docker compose up`, so multiple projects can be seeded side by side. Each file is idempotent (DELETE + INSERT for its own project).

To regenerate from a live project:

```bash
./db/generate-seed.sh <project_id> [--description "override description"]
```

Run without arguments to list available projects.

---

## Testing

```bash
# Unit tests (193 tests)
dotnet test tests/Genesis.AI.Tests/

# Integration tests (WebApplicationFactory + InMemory database)
dotnet test tests/Genesis.AI.IntegrationTests/

# E2E API tests (needs running API + identity service credentials in .env)
dotnet test tests/Genesis.AI.ApiTests/
```

- **xUnit v3 + Moq** for unit tests
- **WebApplicationFactory + EF Core InMemory** for integration tests
- **Refit + ROPC token flow** for E2E API tests
- **MockTokenGenerator** for JWT testing
- Test naming: `Method_Scenario_Expected`

---

## Key Design Decisions

1. **Native PostgreSQL enums** — Type safety at the DB level; never text columns
2. **CQRS without event sourcing** — MediatR for separation; EF Core for persistence
3. **Embedded prompts & skills** — System prompts versioned with code as embedded resources
4. **SSE streaming with tool loop** — Real-time delivery without WebSocket complexity
5. **Soft-delete** — Projects are never physically deleted
6. **Stage reopening** — Completed stages can be re-entered (increments iteration)
7. **Project-scoped artefacts** — Cross-stage visibility; later stages read earlier output
8. **S3 content storage** — Artefact content in S3 (LocalStack locally); DB holds metadata + S3 key only
9. **Project-aggregated parking lot** — Items raised anywhere, resolved anywhere
10. **Up to 40 tool turns** — Generous limit for output-heavy phases (e.g., saving 15+ requirement files)
11. **Prompt caching** — Cache checkpoint after system prompt; 90% cost reduction on repeated context in tool loops
12. **Per-turn token tracking** — Every Bedrock response records input, output, cache read, and cache write tokens with cost estimation
13. **Foundation prompt prefix** — Stable upstream artefacts (Category A) are loaded in full and placed *before* the Bedrock cache point so they are cached across turns (~10× cheaper cached tokens). Mapped per stage by `StageFoundationMap` and assembled by `FoundationService`. Toggled via `TokenOptimisation:FoundationPrefixEnabled`
14. **Per-requirement windowing** — Conversations can be scoped to a single requirement (`requirement_id`), giving each a bounded message window. The AI moves between requirements with the `advance_requirement` tool. Toggled via `TokenOptimisation:RequirementWindowingEnabled`
15. **Explicit orchestration modes** — `forward_sweep` (default, windowed) and `cross_check` (non-windowed holistic pass for P6–P8). The mode is switched explicitly via `set_orchestration_mode`, never inferred. Toggled via `TokenOptimisation:NonWindowedCrossCheckEnabled`

---

## Guardrail Compliance

Platform: `emis-x-api` (ref v2.0.1). Suppressions documented in .guardrail-suppressions.yaml.
