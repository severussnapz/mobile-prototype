# Genesis AI Help Chat — How It Works

## Overview

The help chat is a persistent, project-aware assistant embedded in every page of the Genesis AI tool. It answers two types of questions:

1. **Tool questions** — how the Genesis AI pipeline works, what each stage does, what a CHECK is, how to handle a GAP response, what the EMIS-X UI kit provides
2. **Project questions** — what requirements were captured for this project, what hazards have been identified, what the current project status is, what decisions were made in architecture

Both types of questions are answered from actual indexed content — not from LLM training data. The model cannot hallucinate content that is not in the knowledge base.

---

## End-to-End Flow

### Step 1 — Panel opens

The `HelpChatPanel` React component mounts at Shell level — rendered alongside every page. On mount it calls:

```
GET /api/v1/help/conversations?projectId={id}
```

If an existing `HelpConversation` exists for this user + project, the `helpConversationId` is loaded into state. The conversation continues from where it left off — full history restored. If no conversation exists, one is created on the first message.

### Step 2 — User sends a message

The frontend calls:

```
POST /api/v1/help/stream
{
  "message": "what is a CHECK in a REQ file?",
  "projectId": "d0cf7a10-...",
  "helpConversationId": "abc123..."
}
```

### Step 3 — HelpChatController

The controller:
- Validates the message — rejects empty strings with 400
- Sets `Content-Type: text/event-stream`
- Delegates to `HelpChatStreamService.StreamAsync`

### Step 4 — Knowledge retrieval

Two pgvector queries fire against the `knowledge_document` table:

```
Query 1 — Genesis AI tool knowledge
IKnowledgeService.QueryAsync(
  query: message,
  namespace: KnowledgeNamespace.GenesisTool,
  projectId: null,
  topN: 3
)
→ Bedrock Titan Text Embeddings v2 embeds the query
→ pgvector cosine similarity search against genesis-tool chunks
→ Returns top 3 most relevant chunks from pipeline docs,
  training modules, skills files, API guide

Query 2 — Project artefact knowledge
IKnowledgeService.QueryAsync(
  query: message,
  namespace: KnowledgeNamespace.ProjectArtefact,
  projectId: projectId,  // scoped to this project only
  topN: 5
)
→ Bedrock Titan embeds the query
→ pgvector cosine similarity search against project-artefact chunks
→ Returns top 5 most relevant chunks from approved REQ files,
  ARCH documents, DCB0129 hazard logs, IG records, SEC reviews
  for this specific project
```

Query 2 only fires if `projectId` is present. If the user is not inside a project, only tool knowledge is queried.

### Step 5 — System prompt construction

The retrieved chunks are assembled into a system prompt:

```
You are the Genesis AI help assistant. {context instruction}

## Project Context
{project-artefact chunks — project-specific information first,
 closest to the instruction boundary}

## Genesis AI Knowledge
{genesis-tool chunks — pipeline documentation second}
```

Project artefacts appear first (closer to the instruction boundary) because when a user is inside a project, project-specific answers take priority over general tool knowledge.

### Step 6 — Bedrock call

System prompt + full conversation history + new user message → Bedrock Claude via `IAiService.StreamResponseAsync`. Response streams as `IAsyncEnumerable<string>` chunks.

### Step 7 — SSE streaming

Each chunk is encoded and sent as a Server-Sent Event:

```
data: {chunk with \n encoded as \\n}\n\n
```

The frontend `ReadableStream` reader:
- Splits on `\n`
- Strips `data: ` prefix
- Decodes `\\n` back to `\n`
- Appends to the assistant message in state
- ReactMarkdown renders markdown in real time

### Step 8 — Persistence

After streaming completes:
- User message persisted to `help_message` table
- Assistant response persisted to `help_message` table
- `help_conversation.updated_at` updated
- `HelpConversation` has no `StageId` dependency — it is not tied to any pipeline stage

---

## Knowledge Namespaces

### `genesis-tool` (global)

Contains the Genesis AI pipeline documentation. Seeded on every deployment by `KnowledgeSeederService` — a `BackgroundService` that runs 5 seconds after startup.

**Content sources (embedded markdown in `Genesis.AI.Infrastructure`):**
- `Prompts/` — all 10 pipeline prompts (P01–P10) and policy files
- `Skills/` — all 115 skill files (interview discipline, output protocols, clinical safety methods, etc.)
- `KnowledgeBase/` — platform guide, workstream designs, architecture decisions, coding standards, training modules
- `KnowledgeBase/Training/` — 8 onboarding modules + quick reference card

**Seeder behaviour:**
- On startup: checks whether chunk index 0 exists for each source path
- If exists: skip (already indexed)
- If missing: chunk → embed via Bedrock Titan → store in pgvector
- Uses `CancellationToken.None` for indexing — runs to completion regardless of host shutdown signals
- 5-second startup delay to allow the host to fully initialise

### `project-artefact` (per project)

Contains approved artefacts for a specific project. Indexed at approval time via the `ArtefactPublishedDomainEvent`.

**When an artefact is approved:**
1. `Artefact.CreateS3Artefact(isPublished: true)` or `Artefact.PromoteToPublished()` raises `ArtefactPublishedDomainEvent`
2. `DatabaseContext.SaveChangesAsync` dispatches the event via MediatR before `base.SaveChangesAsync`
3. `ArtefactPublishedDomainEventHandler` fires:
   - Filters: only `text/markdown` and `text/plain` content types (excludes HTML, binary)
   - Fetches content from S3 via `IArtefactStorageService.GetContentAsync`
   - Calls `IKnowledgeService.IndexDocumentAsync` with `KnowledgeNamespace.ProjectArtefact` and the project's `projectId`
   - Best-effort: catches all exceptions, logs, never rethrows — approval never blocked by indexing failure

**Tagged by:** `projectId` — queries are always scoped to one project. A user in the Documents Manager project never sees Prescribing artefacts.

---

## Chunking Strategy

The `BedrockKnowledgeService.ChunkMarkdown` method applies these rules:

**1. Hierarchical breadcrumb prefixing**
Every chunk is prefixed with the full heading path from the document root. So a chunk under `### Exercise 2` inside `## Module 1: Requirements Discovery` inside a document titled `# Genesis AI Onboarding` becomes:

```
Genesis AI Onboarding > Module 1: Requirements Discovery > Exercise 2

{actual chunk content}
```

This means structural queries like "exercise 2 module 1" can match the chunk even when the query terms do not appear in the content.

**2. Chunk overlap**
~30 words from the end of the previous chunk are prepended to the start of the next chunk. This prevents relevant content being split at a section boundary and missed by retrieval.

**3. Target word count**
`TargetWordCount = 150` — chunks flush at paragraph boundaries when this threshold is reached. Smaller chunks improve retrieval precision.

**4. Hard cap**
`HardCapChars = 6000` — any chunk exceeding this is split further regardless of paragraph boundaries.

**5. Code block protection**
Lines inside fenced code blocks (` ``` `) are never split across chunks regardless of word count.

---

## Retrieval Rules

| Rule | Value | Rationale |
|------|-------|-----------|
| `genesis-tool` top-N | 3 | Tool knowledge is high-quality and focused — fewer chunks reduces system prompt bloat |
| `project-artefact` top-N | 5 | Project artefacts are longer and more varied — higher recall needed |
| Max top-N cap | 20 | Hard ceiling regardless of what the caller requests |
| Similarity metric | Cosine distance | Standard for semantic text similarity |
| Embedding model | Bedrock Titan Text Embeddings v2 | 1024 dimensions, sovereign boundary |

---

## How to Get Good Answers from the Help Chat

The help chat uses semantic similarity search — it retrieves chunks whose meaning is closest to the query. This works best with content-specific questions.

**Ask by concept, not by location:**

| Instead of | Ask this |
|-----------|----------|
| "what is exercise 2 of Module 1?" | "how do I handle a GAP response in a P01 session?" |
| "what does Module 3 say about ADRs?" | "what format should an ADR follow in Genesis AI?" |
| "what is in the quick reference?" | "what are the three signals I need to respond to?" |
| "what does the platform guide say about chunking?" | "how does the knowledge service chunk markdown documents?" |

**When inside a project:** the help chat has access to approved artefacts. Ask about specific content: "what requirements did we capture for patient matching?" or "what hazards were identified for the notification feature?"

**When outside a project:** the help chat only has access to Genesis AI tool knowledge. Ask about the pipeline, stages, concepts, and methodology.

---

## Workstream C Plug-In Point

`HelpChatStreamService` calls `IKnowledgeService.QueryAsync`. When the Context Graph (Plan KG) is ready, `IKnowledgeService` is swapped for a Knowledge Graph MCP call — no changes to the controller, the frontend, or the SSE streaming pipeline. One DI registration swap. The help chat gains cross-project patterns, blast radius analysis, and codebase context automatically.

---

*Genesis AI Help Chat Architecture v1.0 | July 2026*
*Next update: when Plan 4c (GitHub integration) and Plan KG (Context Graph) land*
