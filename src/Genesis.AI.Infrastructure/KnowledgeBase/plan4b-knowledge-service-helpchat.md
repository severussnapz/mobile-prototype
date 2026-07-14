# Plan 4b — Knowledge Service + Help Chat Panel

## Goal

Two interconnected deliverables that together give Genesis AI a living knowledge layer and a persistent help interface available from any pipeline stage:

1. **Genesis AI Knowledge Service** — a lightweight pgvector-backed semantic search service inside `genesis-ai-requirements-api`. Two namespaces: Genesis AI tool documentation (global) and project artefacts (per-project). Workstream C plug-in point when the full Knowledge Graph is ready.

2. **Help Chat Panel** — a persistent floating panel in `genesis-ai-requirements-app`, available from any pipeline stage without navigation, backed by the Knowledge Service. Answers "how do I use this?" from tool documentation and "what did we decide?" from project artefacts.

---

## Architecture (confirmed July 2026)

### Genesis AI Knowledge Service

Inside `genesis-ai-requirements-api` — not a separate repo. Owned by the Genesis AI requirements team. When Workstream C (full Knowledge Graph) delivers, `IKnowledgeService` is swapped for a Knowledge Graph MCP client — one clean swap, no rearchitecting.

**Technology:** PostgreSQL with pgvector extension (already in the Postgres estate). Bedrock Titan Text Embeddings v2 for embedding generation (already in the VPC, already approved).

**One table, two namespaces:**

```sql
-- V18__enable_pgvector.sql
CREATE EXTENSION IF NOT EXISTS vector;

-- V19__add_knowledge_documents.sql
CREATE TABLE knowledge_documents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    namespace VARCHAR(50) NOT NULL,
    project_id UUID NULL,
    source_path VARCHAR(500) NOT NULL,
    content TEXT NOT NULL,
    embedding vector(1536) NOT NULL,
    metadata JSONB NOT NULL DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_knowledge_namespace ON knowledge_documents(namespace);
CREATE INDEX idx_knowledge_project ON knowledge_documents(project_id) WHERE project_id IS NOT NULL;
CREATE INDEX idx_knowledge_source ON knowledge_documents(namespace, source_path);
CREATE INDEX idx_knowledge_embedding ON knowledge_documents USING ivfflat (embedding vector_cosine_ops)
    WITH (lists = 100);
```

**`genesis-tool` namespace — seeded on deployment:**
Content sourced from embedded markdown resources in `Genesis.AI.Infrastructure`:
- Pipeline stage descriptions P01–P11
- How-to guidance per stage
- GAP/CLARIFICATION/CONTRADICTION handling
- Common mistakes and terminology
- The user guide (once written)

Updated via PR — governed by CODEOWNERS like any other prompt file. Reseeded on deployment when embedded resources change.

**`project-artefact` namespace — indexed at approval time:**
Every approved artefact (REQ files, hazard logs, architecture docs, prototypes, session close files, PROJECT.md) is chunked and indexed tagged with `projectId`. Re-indexed on every amendment. Deleted from index when a project is deleted.

**Chunking strategy:** 512 token chunks with 64 token overlap. Metadata stored per chunk: `artefact_type`, `stage`, `project_id`, `source_path`, `version`.

**`IKnowledgeService` interface:**
```csharp
public interface IKnowledgeService
{
    Task IndexDocumentAsync(
        string @namespace,
        Guid? projectId,
        string sourcePath,
        string content,
        Dictionary<string, string> metadata,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnowledgeChunk>> QueryAsync(
        string query,
        string @namespace,
        Guid? projectId,
        int topN = 5,
        CancellationToken cancellationToken = default);

    Task DeleteBySourcePathAsync(
        string @namespace,
        Guid? projectId,
        string sourcePath,
        CancellationToken cancellationToken = default);
}

public record KnowledgeChunk(
    string Content,
    string SourcePath,
    double Score,
    Dictionary<string, string> Metadata);
```

**`BedrockKnowledgeService` implementation:**
- `IndexDocumentAsync` — chunks content, calls Bedrock Titan Text Embeddings v2 to embed each chunk, upserts to pgvector (delete existing chunks for source path first, then insert fresh)
- `QueryAsync` — embeds query via Bedrock Titan, runs cosine similarity search against pgvector filtered by namespace and optional projectId, returns top-N chunks ordered by score
- `DeleteBySourcePathAsync` — removes all chunks for a given source path

**`KnowledgeSeederService`** — `IHostedService` that seeds `genesis-tool` namespace on application startup. Idempotent — checks existing source paths, only re-seeds if content hash has changed.

**Workstream C plug-in point:**
Replace `BedrockKnowledgeService` with `KnowledgeGraphMcpClient` that implements `IKnowledgeService`. DI registration swap only. `HelpChatStreamService` and `HelpChatPanel` unchanged.

### Help Chat Panel

**Not a new pipeline stage. Not tied to `PipelineStage`. Not a new route.**

A persistent floating panel rendered once at Shell level in `Routes.tsx` — available from every page without navigation. Toggle button always visible bottom-right of the viewport.

**`HelpConversation` aggregate:**
Dedicated lightweight aggregate. No `StageId`. No phases. No parking lot. No template contracts. Properties: `ProjectId?`, `UserErn`, `CreatedAt`, `UpdatedAt`. Owns a list of `HelpMessage` (role + content + created_at).

```sql
-- V20__add_help_conversations.sql
CREATE TABLE help_conversations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    project_id UUID NULL REFERENCES projects(id) ON DELETE SET NULL,
    user_ern VARCHAR(100) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE help_messages (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    help_conversation_id UUID NOT NULL REFERENCES help_conversations(id) ON DELETE CASCADE,
    role VARCHAR(20) NOT NULL,
    content TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_help_conversations_project ON help_conversations(project_id);
CREATE INDEX idx_help_conversations_user ON help_conversations(user_ern);
CREATE INDEX idx_help_messages_conversation ON help_messages(help_conversation_id);
```

**API endpoints:**

`GET /api/v1/help/conversations?projectId={id}` — returns the most recent help conversation for this user + project combination. Returns null if none exists. Used by the panel on mount to restore existing conversation.

`POST /api/v1/help/stream` — creates or continues a help conversation. Body: `{ message: string, projectId?: string, helpConversationId?: string }`. Returns SSE stream identical in format to the existing conversation stream. Persists the user message and assistant response to `help_messages`.

**`HelpChatStreamService` — query pattern per turn:**
1. Embed the user's message via Bedrock Titan Text Embeddings v2
2. Query `genesis-tool` namespace — top 5 relevant chunks from Genesis AI documentation
3. If `projectId` is present — query `project-artefact` namespace filtered by `projectId` — top 5 relevant chunks from this project's approved artefacts
4. Build system prompt: static header (role, constraints, no-hallucination instruction) + retrieved tool chunks + retrieved project chunks
5. Build message history from `help_messages` for this conversation
6. Call `IAiService.StreamResponseAsync` — same Bedrock infrastructure as every other pipeline stage
7. Persist assistant response to `help_messages`
8. Stream response back to client via SSE

**What the help chat answers correctly (v1):**
- "What should I be doing in P01?" — `genesis-tool` namespace
- "What requirements did we capture around patient matching?" — `project-artefact` namespace
- "Why did we go with Approach B?" — `project-artefact` namespace (decision in approved REQ file)
- "Who approves P06 prompt changes?" — `genesis-tool` namespace (prompt governance docs)
- "What hazards have been identified?" — `project-artefact` namespace (DCB0129 artefact)

**What the help chat cannot answer (v1 — honest):**
- Cross-project questions ("what hazards across all projects?") — requires Workstream C
- External regulatory content (MHRA, NHS Digital standards) — not in the KB yet
- Anything not in tool documentation or this project's approved artefacts

When context is not found, the help chat says so directly and suggests who to ask. It does not hallucinate.

**Frontend — `HelpChatPanel.tsx`:**
Rendered once at Shell level:
```tsx
// In Shell component in Routes.tsx
const { projectId } = useAppLocation();
// ... existing page rendering ...
<HelpChatPanel projectId={projectId} />
```

Component structure:
- Floating `?` button, bottom-right, `z-index` above all page content, always visible
- Click toggles a 400px wide panel, full viewport height, slides in from the right
- Panel header: "Genesis AI Help" + close button
- Message list: user/assistant messages, Markdown rendered, same pattern as `ConversationPage`
- Input fixed at bottom: textarea + send button
- On mount: `GET /api/v1/help/conversations?projectId={id}` — restores existing conversation if present, loads message history
- `helpConversationId` stored in component state — passed on every message to maintain continuity
- Streaming via existing SSE client pattern (`useConversationStream` or equivalent hook)
- No parking lot, no token usage panel, no progress sidebar — chat only
- Panel state (`isOpen`) persists in component memory — does not reset on navigation

---

## Build order

### Day 1 — pgvector + Knowledge Service foundation

**API — tests RED first:**
```
KnowledgeServiceTests:
- IndexDocumentAsync_StoresChunksInCorrectNamespace
- QueryAsync_ReturnsRelevantChunks_FilteredByNamespace
- QueryAsync_WhenProjectIdProvided_FiltersToProjectArtefacts
- DeleteBySourcePathAsync_RemovesAllChunksForSourcePath
- KnowledgeSeeder_Seeds_GenesisToolNamespace_OnStartup
- KnowledgeSeeder_IsIdempotent_WhenContentUnchanged
```

**API — implement:**
- `V18__enable_pgvector.sql`
- `V19__add_knowledge_documents.sql`
- `KnowledgeDocument` entity + EF config (no manual table/column names — convention handles it)
- `IKnowledgeService` interface + `KnowledgeChunk` record
- `BedrockKnowledgeService` implementation
- `KnowledgeSeederService` (`IHostedService`)
- Embedded markdown resources for `genesis-tool` namespace seed content
- DI registration

**Verify:** Build clean, tests GREEN, seeder runs on startup without error.

### Day 2 — Artefact approval hook

**API — tests RED first:**
```
ArtefactApprovalKnowledgeIndexTests:
- ApproveArtefact_IndexesIntoProjectArtefactNamespace
- ApproveArtefact_ReIndexes_WhenArtefactAmended
- DeleteProject_RemovesArtefactsFromKnowledgeIndex
```

**API — implement:**
- Call `IKnowledgeService.IndexDocumentAsync` from the artefact approval handler — every approved artefact indexed into `project-artefact` namespace tagged with `projectId`
- Call `IKnowledgeService.DeleteBySourcePathAsync` on artefact amendment (before re-indexing updated version)
- Call `IKnowledgeService.DeleteBySourcePathAsync` for all project artefacts on project deletion

**Verify:** Build clean, tests GREEN.

### Day 3 — HelpConversation aggregate + API

**API — tests RED first:**
```
HelpChatControllerTests:
- GetConversation_ReturnsExisting_WhenConversationExistsForUserAndProject
- GetConversation_ReturnsNull_WhenNoConversationExists
- Stream_CreatesNewConversation_WhenHelpConversationIdNotProvided
- Stream_ContinuesExisting_WhenHelpConversationIdProvided
- Stream_QueriesBothNamespaces_WhenProjectIdProvided
- Stream_QueriesToolNamespaceOnly_WhenNoProjectId
- Stream_DoesNotHallucinate_WhenNoContextFound
```

**API — implement:**
- `V20__add_help_conversations.sql`
- `HelpConversation` aggregate + `HelpMessage` value object
- `IHelpConversationRepository` + `HelpConversationRepository`
- `HelpChatController` — GET and POST/stream endpoints
- `HelpChatStreamService` — knowledge query + system prompt assembly + Bedrock streaming + message persistence
- DI registration

**Verify:** Build clean, all tests GREEN. 776+ unit + 120+ integration passing.

### Day 4 — HelpChatPanel in the app

**App — tests RED first:**
```
HelpChatPanelTests:
- renders_toggle_button_always_visible
- opens_panel_on_button_click
- closes_panel_on_close_button_click
- restores_existing_conversation_on_mount
- sends_message_and_displays_response
- persists_panel_open_state_across_navigation
```

**App — implement:**
```
src/components/HelpChat/
  HelpChatPanel.tsx
  HelpChatPanel.test.tsx
  HelpChatPanel.module.scss
```

Wire into Shell in `Routes.tsx`:
```tsx
<HelpChatPanel projectId={projectId} />
```

**Verify:** App tests GREEN. tsc clean. lint clean.

### Day 5 — User guide

- Write Genesis AI Pipeline User Guide — what each stage does, what good input looks like, how to handle GAP/CLARIFICATION/CONTRADICTION, common mistakes, help chat usage
- Add as an embedded markdown resource in `Genesis.AI.Infrastructure`
- Included in `KnowledgeSeederService` seed content for `genesis-tool` namespace
- Available as a downloadable document from the tool (static asset)

---

## Success criteria

- A user can ask "what does P06 do?" from any pipeline stage and get an accurate answer grounded in Genesis AI documentation — without navigating away
- A user in a P06 session can ask "what requirements did we capture in P01?" and get an answer grounded in the actual approved REQ file for that project — not a hallucination
- When the help chat cannot find relevant context, it says so directly and does not hallucinate
- Every approved artefact is indexed into the Knowledge Service within 5 seconds of approval
- Help chat conversation history persists across browser sessions and navigation
- Workstream C plug-in: swapping `IKnowledgeService` implementation for a Knowledge Graph MCP client requires no changes to `HelpChatStreamService` or `HelpChatPanel`
- 776+ unit + 120+ integration tests passing after every day. 0 errors. 0 warnings.

---

## What's NOT in Plan 4b

- GitHub artefact push to feature repos — Plan 4c
- P00 project setup form extension — Plan 4c
- SESSION-CLOSE button — Plan 4c
- P06 Excel export — Plan 4c
- CODEOWNERS file — Plan 4c
- Cross-project knowledge queries — requires Workstream C Knowledge Graph
- Confluence ingestion — Workstream C Phase 3a
- External regulatory content (MHRA, NHS Digital standards) — Workstream C Phase 4
- Medical Device pipeline (P09) — design session with Indra required first
- Project dashboard (KPIs/OKRs) — design session required first
- Figma Option A — Wave H, after Plan 4 production flag flip
- Pipeline chat cross-stage artefact access (prompt update) — after CODEOWNERS in place (Plan 4c)
