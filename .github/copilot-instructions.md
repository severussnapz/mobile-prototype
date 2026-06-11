<!-- rtk-instructions v2 -->
# RTK — Token-Optimized CLI

**rtk** is a CLI proxy that filters and compresses command outputs, saving 60-90% tokens.

## Rule

Always prefix shell commands with `rtk`:

```bash
# Instead of:              Use:
git status                 rtk git status
git log -10                rtk git log -10
cargo test                 rtk cargo test
docker ps                  rtk docker ps
kubectl get pods           rtk kubectl pods
```

## Meta commands (use directly)

```bash
rtk gain              # Token savings dashboard
rtk gain --history    # Per-command savings history
rtk discover          # Find missed rtk opportunities
rtk proxy <cmd>       # Run raw (no filtering) but track usage
```
<!-- /rtk-instructions -->

# Genesis AI Requirements API — Copilot Instructions

## Project Overview

Backend REST API for the Genesis AI Requirements Platform. Orchestrates an 8-stage requirements pipeline through AI-driven conversations, artefact generation, and stage management.

**Tech Stack:** .NET 10.0, ASP.NET Core, Entity Framework Core, PostgreSQL 17, MediatR (CQRS), AutoMapper, FluentValidation, AWS Bedrock (Claude Sonnet 4.6), AWSSDK.S3 (LocalStack locally), Docker, Flyway migrations.

---

## Architecture

### Layer Structure

```
src/
├── Genesis.AI.Api/              # HTTP presentation layer
│   ├── Authentication/          # Policies, scopes, claims extensions (cross-cutting)
│   ├── Http/                    # Shared HTTP envelopes: ApiResponse<T>, ApiErrorResponse, ApiError
│   ├── Middleware/              # Response headers (SEC-005)
│   ├── Features/                # Vertical slices — one folder per domain feature
│   │   ├── Projects/            # ProjectsController, ProjectResource, PipelineStageResource,
│   │   │                        # CreateProjectRequest, ProjectMappingProfile,
│   │   │                        # ProjectTokenUsageResponse, TokenUsageTotals
│   │   ├── Conversations/       # ConversationsController, ConversationStreamController,
│   │   │                        # ConversationStateController, ConversationResource,
│   │   │                        # MessageResource, ConversationProgressResponse,
│   │   │                        # ParkingLotItemResponse, PhaseResponse, MessageCreatedResponse,
│   │   │                        # request models, attachment types, ConversationMappingProfile
│   │   ├── Artefacts/           # ArtefactController, ArtefactSummaryResponse,
│   │   │                        # ArtefactDetailResponse, CreateArtefactsRequest,
│   │   │                        # CreateArtefactRequestItem
│   │   ├── Stages/              # PipelineStagesController, StageStatusResponse, StageMessageResponse
│   │   └── Export/              # ProjectExportController
├── Genesis.AI.Core/             # Shared base types
│   ├── Data/                    # (reserved)
│   ├── Domain/                  # Entity base class, IAggregateRoot
│   ├── Filters/                 # IExceptionLoggingFilter (OBS-003)
│   └── Logging/                 # Serilog configuration (OBS-002)
├── Genesis.AI.Domain/           # Business logic (pure, no infrastructure deps)
│   ├── AggregatesModel/         # Aggregate roots + child entities
│   │   ├── ArtefactAggregate/
│   │   ├── ConversationAggregate/   # Conversation, Message, ParkingLotItem
│   │   └── ProjectAggregate/        # Project, PipelineStage
│   ├── Commands/                # Write operations (one folder per command)
│   ├── Queries/                 # Read operations (one folder per query)
│   ├── Enums/                   # Domain enums (all mapped to PostgreSQL native types)
│   └── Interfaces/              # Repository + service contracts, AI DTOs
└── Genesis.AI.Infrastructure/   # Data access + external services
    ├── EntityConfigurations/    # EF Core fluent API config
    ├── Repositories/            # IProjectRepository, IConversationRepository, IArtefactRepository
    ├── Services/                # BedrockAiService, EmbeddedPromptService, SkillContentService, PipelineToolDefinitions, S3ArtefactStorageService
    ├── Skills/                  # Embedded skill content (.md files) injected into AI context
    └── Prompts/                 # Stage-specific system prompts (embedded resources)
```

### CQRS Pattern

All business logic flows through MediatR handlers:

- **Commands** (writes): `Domain/Commands/{Feature}/` — contains `Command.cs`, `CommandHandler.cs`, optionally `CommandValidator.cs`
- **Queries** (reads): `Domain/Queries/{Feature}/` — contains `Query.cs`, `QueryHandler.cs`
- Handlers return domain entities; controllers map to response types via AutoMapper

### Vertical Slice Organisation (ARCH-008)

The `Genesis.AI.Api` layer is organised by **feature slice**, not by technical role:
- Each `Features/{Feature}/` folder contains its controller(s), request models, response models, resources, and AutoMapper profile
- Shared HTTP infrastructure (response envelopes, error types) lives in `Http/` — not in any feature slice
- Cross-cutting concerns (auth policies, middleware) remain at the `Api` root
- **No** `Controllers/`, `Dtos/`, `Requests/`, `Resources/`, or `Mapping/` folders — these are replaced by feature slices

### Domain Model

**Aggregate Roots:**
- `Project` — Contains `PipelineStage` collection. Auto-initialises 8 stages on creation. Supports soft-delete.
- `Conversation` — Contains `Message` and `ParkingLotItem` collections. Tracks phase progress, questions asked, requirements captured.
- `Artefact` — Metadata stored in DB; content stored in S3 (LocalStack locally). Properties: `S3Key`, `ContentType`, `SizeBytes`. Use `CreateS3Artefact` factory. Versioned per file path per project.

**Child Entities (not aggregate roots):**
- `PipelineStage` — Owned by Project. State machine: NotStarted → InProgress → Complete (or Blocked).
- `Message` — Owned by Conversation. Stores role, content, token count, user identity (`UserErn`, `GivenName`, `FamilyName`), optional image/document attachments (JSONB).
- `ParkingLotItem` — Owned by Conversation. Stores content, priority, status, source phase.
- `TokenUsageRecord` — Owned by Conversation. Stores input, output, cache read, and cache write token counts per AI turn.

**Parking Lot Scoping:**
- Items are stored per-conversation (FK `conversation_id`) but are conceptually project-scoped.
- `GET /api/v1/projects/{id}/parking-lot` aggregates items across all conversations in the project.
- Conversation-level endpoints (`/conversations/{id}/parking-lot`) are used during active AI sessions.
- `ParkingLotItemResponse` includes `ConversationId` so the frontend can call conversation-level mutation endpoints (resolve/defer/delete) from the project-level view.

**Key Behaviours:**
- `Project` constructor auto-creates 8 pipeline stages with correct sort order; only RequirementsDiscovery starts as NotStarted, all others start Blocked
- `Project.RecalculateStatus()` calls `UnblockAvailableStages()` which checks prerequisites and transitions Blocked → NotStarted when dependencies are satisfied
- `PipelineStage.Start()` / `.Complete()` / `.Skip()` / `.Block()` / `.Reopen()` — state transitions with iteration tracking
- `Conversation.AddMessage()` / `.AdvancePhase()` / `.SetPhase()` / `.UpdateProgress()` / `.Complete()` / `.Pause()` / `.Resume()`
- `ParkingLotItem.Resolve()` / `.Defer()` / `.UpdatePriority()` / `.UpdateContent()`
- Stage reopening increments `Iteration` and resets status to InProgress
- Always fix forward dont fake passing tests by supressing always find a solution

---

## Authentication & Authorisation

### Scopes

| Scope | Purpose |
|-------|---------|
| `genai-req.admin` | Full access |
| `genai-req.read` | Read projects, conversations, artefacts |
| `genai-req.write` | Create/modify projects, conversations |
| `genai-req.arch` | Converse on Architecture stage |
| `genai-req.pxd` | Converse on Product Design stage |
| `genai-req.clin` | Converse on Clinical Safety stage |

### Policies

| Policy | Accepts scopes |
|--------|---------------|
| `ProjectRead` | read, write, admin |
| `ProjectWrite` | write, admin |
| `ConversationRead` | read, write, admin, arch, pxd, clin |
| `ConversationWrite` | write, admin, arch, pxd, clin |
| `ArchitectureConverse` | arch, admin |
| `ProductDesignConverse` | pxd, admin |
| `ClinicalSafetyConverse` | clin, admin |

Every controller action MUST have `[Authorize(Policy = "...")]` or `[AllowAnonymous]` (AUTH-004 guardrail).

---

## Key Conventions

### Naming

| Context | Convention | Example |
|---------|------------|---------|
| C# classes/properties | PascalCase | `PipelineStage`, `StageType` |
| Database columns | snake_case | `pipeline_stage`, `stage_type` |
| JSON/API responses | camelCase | `pipelineStage`, `stageType` |
| Private fields | _camelCase | `_context`, `_repository` |
| Enums (C#) | PascalCase values | `StageType.RequirementsDiscovery` |
| Enums (PostgreSQL) | snake_case values | `'requirements_discovery'` |
| Lambda parameters | Descriptive names | `.Where(artefact => ...)` not `.Where(a => ...)` |

### Database Enums (CRITICAL)

**Always use native PostgreSQL enum types.** Never use text columns for enums.

All enums must be registered at **three** C# levels (in `DependencyInjection.cs`):
1. `dataSourceBuilder.MapEnum<T>("pg_type_name")` — Npgsql driver serialisation
2. `options.UseNpgsql(dataSource, o => o.MapEnum<T>("pg_type_name"))` — EF Core SQL generation
3. `modelBuilder.HasPostgresEnum<T>()` — EF Core model metadata (in `GenesisAiDbContext`)

C# enum values use `[PgName("snake_case_value")]` attributes for PostgreSQL label mapping.

Default values use `HasDefaultValueSql("'value'::enum_type")`, never `HasDefaultValue(Enum.Value)`.

### Current Enums

| C# Type | PostgreSQL Type | Values |
|---------|----------------|--------|
| `ComplianceDomain` | `compliance_domain` | clinical_uk, generic, finance |
| `ProjectStatus` | `project_status` | discovery, in_progress, complete, archived |
| `StageType` | `stage_type` | requirements_discovery, prototype, architecture, design, pxd, clinical_safety, normalisation, planning |
| `PipelineStageStatus` | `pipeline_stage_status` | not_started, in_progress, complete, blocked |
| `ConversationStatus` | `conversation_status` | active, completed, paused |
| `ParkingLotPriority` | `parking_lot_priority` | critical, high, medium |
| `ParkingLotStatus` | `parking_lot_status` | open, resolved, deferred |
| `MessageRole` | `message_role` | user, assistant, system |

### Code Structure Rules

- One class per file (CS-001 guardrail)
- Namespace matches folder path — e.g. `Genesis.AI.Api.Features.Projects`
- Controllers are thin — delegate to MediatR immediately
- All controller body return types must be concrete named types — no `new { ... }` anonymous objects (API-017 guardrail)
- Request/response models live in the feature slice folder alongside their controller (ARCH-007 guardrail)
- Shared HTTP envelopes (`ApiResponse<T>`, `ApiErrorResponse`) live in `Http/` — not in feature slices
- No single-letter lambda parameters (ENG-011 guardrail)
- Test names follow `Method_Scenario_Expected` three-part convention (TEST-007 guardrail)

---

## Database

### Migrations

Location: `db/migrations/` (Flyway format: `V{version}__description.sql`)

Current migrations:
- `V1__initial_schema.sql` — Initial schema (tables, enums, indexes)
- `V2__artefact_content_to_s3.sql` — Drops `content` column; adds `s3_key`, `content_type`, `size_bytes`

### Adding Migrations

1. Create the next versioned file, e.g. `db/migrations/V3__description.sql`
2. Use native PostgreSQL enums (`CREATE TYPE ... AS ENUM`) for any new enum types
3. Update EF Core entity configuration in `Infrastructure/EntityConfigurations/`
4. Register new enums in `DependencyInjection.cs` at all three levels
5. Rebuild: `docker compose up -d --build api`

### Seed Data

`db/seeds/<project-code>.sql` — Idempotent (DELETE + INSERT) per-project seed files. Each creates a test project with conversations, messages, parking lot items, token usage records, notes, decisions, and artefacts. The Docker Compose `seed` service runs every `db/seeds/*.sql` file on boot, so multiple projects can be seeded side by side.

`db/generate-seed.sh` — Generates `db/seeds/<project-code>.sql` from any project in the running database (and extracts its S3 artefacts into `db/seed-artefacts/`). Usage: `./db/generate-seed.sh <project_id> [--description "override"]`. Lists available projects if no ID given.

---

## Running Locally

```bash
# Start everything (postgres → flyway → localstack → seed → api)
docker compose up -d --build

# API available at http://localhost:5000
# Database at localhost:5432 (postgres/postgres, db: genesis_ai_requirements)
# LocalStack S3 at http://localhost:4566 (bucket: genesis-ai-artefacts)

# Rebuild after code changes
docker compose up -d --build api

# View logs
docker compose logs -f api

# Connect to DB
docker compose exec postgres psql -U postgres -d genesis_ai_requirements

# Swagger UI
# http://localhost:5000/swagger
```

### Docker Compose Services

`postgres:17.4-alpine` (5432) → `flyway:12.1.0-alpine` (migrations) → `localstack` (4566, S3 emulation) → `seed` (DB rows + S3 objects) → `api` (5000→8080, `Dockerfile.dev`)

Requires `.env` file with `IDENTITY_URL`, `AUDIENCE`, and credentials (`JFROG_USER`, `JFROG_TOKEN`, `GIT_TOKEN`).

---

## Testing

```bash
# Unit tests (193 tests)
dotnet test tests/Genesis.AI.Tests/

# Integration tests (WebApplicationFactory + InMemory database + mock IArtefactStorageService)
dotnet test tests/Genesis.AI.IntegrationTests/

# E2E API tests (needs running API + identity service credentials in .env)
dotnet test tests/Genesis.AI.ApiTests/
```

- **xUnit v3 + Moq** for unit tests
- **WebApplicationFactory + EF Core InMemory** for integration tests; `IArtefactStorageService` replaced by an in-memory mock (`ConcurrentDictionary<string, string>`) registered in `TestWebApplicationFactory`
- **Refit + ROPC token flow** for E2E API tests
- **MockTokenGenerator** in `Genesis.AI.TestFramework` project
- Test naming: `Method_Scenario_Expected` (three parts separated by underscores)

---

## API Endpoints

### Projects (`/api/v1/projects`)

| Method | Path | Policy | Purpose |
|--------|------|--------|---------|
| POST | `/projects` | ProjectWrite | Create project (auto-creates 8 stages) |
| GET | `/projects` | ProjectRead | List projects (optional `?status=` filter) |
| GET | `/projects/{id}` | ProjectRead | Get project with stages |
| DELETE | `/projects/{id}` | ProjectWrite | Soft-delete |
| GET | `/projects/{id}/parking-lot` | ProjectRead | Aggregate parking lot across all conversations |
| GET | `/projects/{id}/token-usage` | ProjectRead | Aggregated token usage + estimated cost per stage |
| GET | `/projects/{id}/export` | ProjectRead | Download ZIP of all artefacts |

### Artefacts (`/api/v1/projects/{projectId}/artefacts`)

| Method | Path | Policy | Purpose |
|--------|------|--------|---------|
| GET | `/projects/{id}/artefacts` | ProjectRead | List all artefacts for project |
| GET | `/projects/{id}/artefacts/{artefactId}` | ProjectRead | Get artefact with content |
| POST | `/projects/{id}/artefacts` | ProjectWrite | Save one or more artefacts |

### Conversations (`/api/v1/conversations`)

| Method | Path | Policy | Purpose |
|--------|------|--------|---------|
| POST | `/conversations` | ConversationWrite | Create conversation for a stage |
| GET | `/conversations/{id}` | ConversationRead | Get conversation with messages |
| GET | `/conversations/by-stage/{stageId}` | ConversationRead | List conversations for stage |
| POST | `/conversations/{id}/stream` | ConversationWrite | Send message (SSE streaming) |

### Conversation State (`/api/v1/conversations/{id}/...`)

| Method | Path | Policy | Purpose |
|--------|------|--------|---------|
| GET | `/conversations/{id}/progress` | ConversationRead | Get phase progress |
| POST | `/conversations/{id}/advance-phase` | ConversationWrite | Advance to next phase |
| PATCH | `/conversations/{id}/phase` | ConversationWrite | Set specific phase |
| GET | `/conversations/{id}/parking-lot` | ConversationRead | List parking lot items |
| POST | `/conversations/{id}/parking-lot` | ConversationWrite | Add parking lot item |
| POST | `/conversations/{id}/parking-lot/{itemId}/resolve` | ConversationWrite | Resolve item |
| POST | `/conversations/{id}/parking-lot/{itemId}/defer` | ConversationWrite | Defer item |
| DELETE | `/conversations/{id}/parking-lot/{itemId}` | ConversationWrite | Delete item |

### Pipeline Stages (`/api/v1/stages`)

| Method | Path | Policy | Purpose |
|--------|------|--------|---------|
| POST | `/stages/{stageId}/complete` | ProjectWrite | Mark stage complete |
| POST | `/stages/{stageId}/skip` | ProjectWrite | Skip a stage |

---

## S3 Artefact Storage

Artefact content lives in S3; the `artefacts` table stores metadata and the S3 key only.

- **Interface:** `IArtefactStorageService` (`Domain/Interfaces/`) — `SaveContentAsync`, `GetContentAsync`, `DeleteContentAsync`
- **Implementation:** `S3ArtefactStorageService` (`Infrastructure/Services/`) — uses `IAmazonS3`; bucket name from `S3:ArtefactBucketName` config
- **Storage key scheme:** `projects/{projectId}/artefacts/{filePath}/v{version}` (leading `/` stripped from `filePath`)
- **Local development:** LocalStack container at `http://localhost:4566`; `localstack/init-s3.sh` creates the `genesis-ai-artefacts` bucket on startup; in-container URL is `http://localstack:4566`
- **Seed data:** `db/seeds/*.sql` + the seed service upload artefact content objects to LocalStack so the full read path works locally
- **Config keys:**
  - `S3:ArtefactBucketName` — bucket name (e.g. `genesis-ai-artefacts`)
  - `S3:ServiceUrl` — override endpoint (set to LocalStack URL in non-production environments)
- **Never** store artefact content in the database. The `content` column was removed in `V2__artefact_content_to_s3.sql`.

---

## AI Integration

- **Provider:** AWS Bedrock → Claude Sonnet 4.6 (via `AWSSDK.BedrockRuntime`)
- **Service:** `BedrockAiService` implements `IAiService` — handles multi-turn tool loop with `IAsyncEnumerable<AiStreamEvent>`
- **Prompt caching:** Cache checkpoint placed after the system prompt block to avoid re-processing the large system prompt on every tool loop turn (90% cost reduction on cached tokens)
- **Prompts:** `EmbeddedPromptService` — system prompts per stage type stored as embedded `.md` resources in `Infrastructure/Prompts/`
  - `Pipeline01RequirementsDiscovery.md` — Structured requirements interview (13 phases)
  - `Pipeline02Prototype.md` — Generates single-file HTML prototypes for requirements validation
  - `Pipeline03Architecture.md` — BDAT analysis, ADRs, failure modes
  - `Pipeline04Design.md` — API contracts, database schema, component interfaces
  - `Pipeline05Pxd.md` — Product experience design review
  - `Pipeline06ClinicalSafety.md` — DCB0129 hazard analysis
  - `Pipeline07Normalisation.md` — Cross-cutting extraction and normalisation
  - `Pipeline08Planning.md` — Task generation and dependency ordering
- **Skills:** `SkillContentService` — loads guardrail/steer skill content from `Infrastructure/Skills/` for injection into AI context
- **Tool definitions:** `PipelineToolDefinitions` — defines AI tools (save_artefact, advance_phase, update_progress, add_parking_lot_item, resolve_parking_lot_item, list_artefacts, get_artefact, get_guardrail_details)
- **Streaming:** SSE via `text/event-stream` content type from `ConversationStreamController`
- **Tool loop:** Up to 40 tool turns per message (generous limit for output-heavy phases saving 15+ files)

### How a Message Is Processed

1. User sends message → controller adds it to conversation, builds full message history
2. **System prompt assembled** from: base stage prompt + project context + session state + artefact manifest + staleness notice (if applicable)
3. AI streams text chunks → SSE `data: {"text": "..."}` events sent immediately
4. AI invokes tool(s) → controller executes each, persists to DB, sends SSE event
5. Tool results are sent back to AI as continuation messages (assistant + user turn pair)
6. AI produces more text/tools → loop continues until no more tool calls
7. Final text stored as assistant message, `data: [DONE]` sent

### Context Injection (Per Message)

Every message includes these in the system prompt:

| Section | Source | Purpose |
|---------|--------|---------|
| Base prompt | `Infrastructure/Prompts/{stage}.md` | Interview structure, phase definitions |
| Project context | Project record | Name, code, description, compliance domain |
| Session state | Conversation record | Current phase, questions asked, parking lot items |
| Artefact manifest | Artefact table | File paths + versions (LLM uses tools to read content) |
| Staleness notice | Timestamp comparison | Warns LLM if artefacts changed since last message |

### Staleness Detection (Cross-Stage Awareness)

When a user returns to a stage after other stages have modified artefacts:

1. Controller compares `latest artefact timestamp` against `last message timestamp` in this conversation
2. If artefacts are newer → injects warning into system prompt:
   > ⚠️ ARTEFACTS UPDATED: Project artefacts have been modified since your last message in this conversation. Use `list_artefacts` and `get_artefact` to reload any files you previously referenced — they may have changed.
3. LLM sees the warning + the updated manifest, uses `list_artefacts` / `get_artefact` tools to reload changed content
4. This ensures the LLM never works with stale data when stages are revisited out of order

### Available AI Tools

| Tool | Purpose | Side Effect |
|------|---------|-------------|
| `save_artefact` | Save a file (manifest.md, REQ-*.md, etc.) | Creates/versions artefact in DB |
| `advance_phase` | Move to next interview phase | Updates conversation phase tracking |
| `update_progress` | Report questions asked / estimated total | Updates conversation progress metrics |
| `add_parking_lot_item` | Defer a topic for later | Creates parking lot item on conversation |
| `resolve_parking_lot_item` | Mark a parking lot item as resolved | Updates item status to resolved |
| `list_artefacts` | Discover what files exist | Read-only |
| `get_artefact` | Read a specific file's content | Read-only |
| `get_guardrail_details` | Load a skill/guardrail document | Read-only |

### SSE Streaming Events

The `/api/v1/conversations/{id}/stream` endpoint sends real-time events:

| Event | Trigger | Payload |
|-------|---------|--------|
| `data: {"text": "..."}` | AI text chunk | Incremental text content |
| `event: tool_start` | Before each tool executes | `{tool, description}` — human-readable status (e.g. "Saving prototype/index.html...") |
| `event: progress` | `update_progress` or `advance_phase` tool | `{currentPhase, phaseName, totalPhases, questionsAsked, estimatedTotalQuestions, requirementsCaptured}` |
| `event: artefact` | `save_artefact` tool | `{filePath, version, id}` |
| `event: parking_lot_item` | `add_parking_lot_item` tool | `{id, content, priority, status, sourcePhase}` |
| `event: parking_lot_resolved` | `resolve_parking_lot_item` tool | `{id, status}` |
| `event: usage` | End of each AI turn | `{inputTokens, outputTokens, totalTokens, cacheReadInputTokens, cacheWriteInputTokens, cumulativeInputTokens, cumulativeOutputTokens}` |
| `event: error` | AI stream error | `{error, reason}` — AI generation failure |
| `data: [DONE]` | Stream complete | End-of-stream marker |

All tool-triggered events are sent **immediately** when the tool completes (not batched at end of stream).
`tool_start` events fire **before** execution, giving the frontend real-time feedback about what the LLM is doing.

---

## Guardrail Compliance

Platform: `emis-x-api` (defined in `.genesis-ai.yaml`, ref v2.0.1). Run the guardrail analyser before raising a PR.

Suppressions are documented in `.guardrail-suppressions.yaml` with justifications for each.

---

## Important Rules

1. **Never destroy the database volume** without explicit permission — seed data takes effort to recreate
2. **Always use native PostgreSQL enums** — never text columns with HasConversion
3. **Register enums at all three levels** — driver, UseNpgsql options, HasPostgresEnum
4. **Soft-delete only** — Projects set `IsDeleted = true`, never physically deleted
5. **Stage reopening** — Completed stages can be re-entered; increments iteration counter
6. **Stage prerequisites** — Prototype requires RequirementsDiscovery complete; Architecture/Design/Pxd require Prototype complete; ClinicalSafety requires Architecture, Design, AND Pxd all complete (permanently blocked for non-clinical domains); Normalisation requires Arch+Design+Pxd complete and ClinicalSafety complete or permanently blocked; Planning requires Normalisation complete
7. **Conventional commits** — `feat:`, `fix:`, `refactor:`, etc.
8. **Every action needs auth** — `[Authorize(Policy = "...")]` on all controller actions (AUTH-004)
9. **Descriptive lambda parameters** — no single-letter params like `a`, `c`, `x` (ENG-011)
10. **Three-part test names** — `Method_Scenario_Expected` (TEST-007)
11. **British English** — behaviour, colour, organisation (ENG-001)
12. **Artefact content belongs in S3** — never store content in the `artefacts` DB table; use `IArtefactStorageService` to read/write; `Artefact` entity holds `S3Key`, not content
13. **`CreateS3Artefact` is the only factory** — the old `CreateTextArtefact` factory was removed; always use `Artefact.CreateS3Artefact(...)` when creating artefact entities
14. **Feature slices, not technical folders** — new controllers, request models, response models go in `Features/{FeatureName}/`; no `Controllers/`, `Dtos/`, `Requests/`, `Resources/`, or `Mapping/` folders (ARCH-008)
15. **Response models need concrete types** — no `new { ... }` anonymous objects in controller body helpers (API-017); request/response models must live in a feature slice or `Http/` (ARCH-007)