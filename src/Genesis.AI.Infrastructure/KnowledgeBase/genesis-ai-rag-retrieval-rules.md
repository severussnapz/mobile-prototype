# Genesis AI — Knowledge Service: RAG Retrieval Rules and Chunking Strategy

## What This Document Is

This document defines the rules governing how Genesis AI indexes, chunks, and retrieves knowledge. It is the authoritative reference for:
- How markdown documents are chunked before indexing
- How retrieval queries are constructed and ranked
- What rules govern the quality of retrieved context
- How to diagnose and improve retrieval quality

This document applies to both namespaces: `genesis-tool` (global pipeline knowledge) and `project-artefact` (per-project approved artefacts).

---

## The RAG Pipeline

```
Document → ChunkMarkdown → BedrockEmbeddingService → pgvector
                                                          ↓
User query → EmbedAsync → pgvector cosine search → top-N chunks
                                                          ↓
                                             BuildSystemPrompt → Bedrock
```

Every document is processed identically — whether it is a pipeline prompt, a skill file, a training module, or an approved REQ artefact.

---

## Chunking Rules

### Rule 1: Hierarchical Breadcrumb Prefixing

**Every chunk is prefixed with the full heading path from the document root.**

When a markdown document has headings at multiple levels, every chunk produced from a section includes the complete heading hierarchy as a breadcrumb prefix:

```
{H1 title} > {H2 section} > {H3 subsection}

{actual chunk content}
```

**Example:**

A training module with this structure:
```markdown
# Genesis AI — Module 1: Requirements Discovery

## Exercise 2: Handle a GAP Response

During your P01 session, the agent will raise a GAP...
```

Produces a chunk that starts with:
```
Genesis AI — Module 1: Requirements Discovery > Exercise 2: Handle a GAP Response

During your P01 session, the agent will raise a GAP...
```

**Why this matters:** Structural queries like "exercise 2 module 1" or "what does Module 1 say about GAP responses?" now match the chunk because the heading hierarchy appears in the embedded text — not just in the document structure.

**Breadcrumb rules:**
- Heading markdown syntax is stripped — `## Exercise 2` becomes `Exercise 2`
- All heading levels are tracked (H1, H2, H3)
- When a heading of level N is encountered, all headings at level N and deeper are removed from the stack before the new heading is added
- If no headings exist in the document, no breadcrumb prefix is added

### Rule 2: Chunk Overlap

**~30 words from the end of the previous chunk are prepended to the start of the next chunk.**

Chunk boundaries at heading boundaries prevent relevant content being split and missed. The overlap window ensures that content near a section boundary appears in both the preceding and following chunk.

**Why this matters:** Without overlap, a sentence at the end of one section and the beginning of the next section are in different chunks with no shared context. Retrieval may return one and miss the other.

**Overlap rules:**
- Target: last 30 words of the previous chunk (approximately 20% of the 150-word target)
- Overlap is added when a new heading starts a new chunk
- Overlap is not added at paragraph-boundary flushes within a section
- Overlap content is prepended before the new section's content

### Rule 3: Target Word Count — 150 Words

**`TargetWordCount = 150`**

Chunks flush at paragraph boundaries (blank lines) when the accumulated word count reaches 150. Smaller chunks improve retrieval precision — a 150-word chunk is focused enough to answer a specific question without returning irrelevant surrounding content.

**Why 150 and not 400:** The previous value (400 words) produced chunks that were too broad. A 400-word chunk covering an entire section returned too much irrelevant content alongside the relevant bit. At 150 words, a chunk typically covers one concept, one exercise, or one decision — the right granularity for the help chat's use case.

### Rule 4: Hard Cap — 6000 Characters

**`HardCapChars = 6000`**

Any chunk exceeding 6000 characters is split further regardless of paragraph boundaries. This prevents oversized chunks from exceeding the Bedrock embedding model's input limits and from dominating the system prompt.

### Rule 5: Code Block Protection

**Lines inside fenced code blocks are never split across chunk boundaries.**

When the chunker encounters ` ``` `, it sets a flag that prevents paragraph-boundary flushes. Code blocks are always kept intact in a single chunk, regardless of word count.

**Why this matters:** Splitting a code block produces two chunks that are syntactically invalid independently. Neither chunk would match a query about the code correctly.

---

## Embedding Rules

**Model:** AWS Bedrock Titan Text Embeddings v2
**Dimensions:** 1024
**Scope:** All inference through AWS Bedrock via PrivateLink — nothing leaves the VPC

Every chunk is embedded independently. The embedding captures the semantic meaning of the chunk content including the breadcrumb prefix — so the structural context (which module, which section, which exercise) is encoded in the vector alongside the content meaning.

**Batch embedding:** For indexing operations, all chunks for a document are embedded before any DB write transaction begins. This prevents holding a write lock during Bedrock calls. If embedding fails mid-batch, no partial writes occur.

---

## Retrieval Rules

### Query Embedding

The user's query is embedded using the same Bedrock Titan model before the pgvector search. The query vector is compared against all chunk vectors in the target namespace using cosine similarity.

**Cosine distance formula:** Lower distance = higher similarity. pgvector returns results ordered by ascending cosine distance (most similar first).

### Top-N Rules

| Namespace | topN | Rationale |
|-----------|------|-----------|
| `genesis-tool` | 3 | Tool knowledge is high-quality, focused, and curated. 3 chunks are sufficient to answer most pipeline questions without bloating the system prompt |
| `project-artefact` | 5 | Project artefacts are longer, more varied, and require higher recall. 5 chunks are needed to surface relevant content across REQ files, ARCH docs, hazard logs etc. |
| Max cap | 20 | Hard ceiling applied by `ClampTopN` regardless of what the caller requests |

### Namespace Scoping

- `genesis-tool` queries use `projectId = null` — content is global, not project-specific
- `project-artefact` queries filter strictly by `projectId` — a user in one project never retrieves artefacts from another project
- If `projectId` is null (user is not inside a project), the `project-artefact` query is skipped entirely

### System Prompt Ordering

Retrieved chunks are assembled into the system prompt in this order:

```
## Project Context
{project-artefact chunks — project-specific information}

## Genesis AI Knowledge  
{genesis-tool chunks — pipeline documentation}
```

**Project context appears first** — closer to the instruction boundary. Research shows that content placed later in the context (closer to the instruction) has higher influence on the model's response. When a user is inside a project, project-specific answers take priority over general tool knowledge.

---

## Seeding Rules — `genesis-tool` Namespace

### What Gets Seeded

All embedded markdown resources in `Genesis.AI.Infrastructure`:
- `Prompts/*.md` — P01–P10 pipeline prompts + policy files
- `Skills/*.md` — 115 skill files
- `KnowledgeBase/*.md` — platform guides, workstream designs, architecture decisions
- `KnowledgeBase/Training/*.md` — 8 onboarding modules + quick reference card

Resources containing `_ai_new_tmp` in the path are excluded.

### Seeder Behaviour

1. On API startup: `KnowledgeSeederService` fires after a 5-second delay
2. For each embedded `.md` resource: check whether chunk index 0 exists in `knowledge_document` for this `source_path`
3. If exists: skip (already indexed)
4. If missing: chunk → embed → insert all chunks for this document

**Cancellation token:** The startup delay respects the host's stopping token. The seeding work itself uses `CancellationToken.None` — it runs to completion regardless of host shutdown signals. This prevents mid-seed cancellation when re-indexing large document sets.

### When to Force Re-Seed

The seeder's existence check (`chunk_index == 0`) means changes to document content are NOT detected automatically. To force re-indexing after a chunking strategy change or content update:

```sql
DELETE FROM knowledge_document WHERE namespace = 'genesis_tool';
```

Then restart the API. The seeder will re-index all resources with the current chunking strategy.

**Warning:** This deletes all `genesis-tool` chunks. The seeder will re-index on next startup but this takes several minutes and requires valid AWS credentials.

---

## Indexing Rules — `project-artefact` Namespace

### When Indexing Fires

`ArtefactPublishedDomainEventHandler` fires on every `ArtefactPublishedDomainEvent`:

**Indexable content types:**
- `text/markdown` ✅
- `text/plain` ✅

**Non-indexable (skipped silently):**
- `text/html` — tag soup, low signal
- `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` (xlsx) — binary
- `application/vnd.openxmlformats-officedocument.wordprocessingml.document` (docx) — binary
- Any other content type

### Indexing Behaviour

1. Fetch content from S3 via `IArtefactStorageService.GetContentAsync`
2. Skip if content is null or whitespace
3. Call `IKnowledgeService.IndexDocumentAsync` with:
   - `namespace: KnowledgeNamespace.ProjectArtefact`
   - `projectId: artefact.ProjectId`
   - `sourcePath: artefact.FilePath` (version-independent — same path for all versions)
4. `IndexDocumentAsync` performs an atomic delete-then-insert:
   - Delete all existing chunks for this `sourcePath` + `projectId` combination
   - Insert fresh chunks from the new content
5. Best-effort: catches all exceptions, logs, never rethrows — approval is never blocked by indexing failure

### Version Handling

The `sourcePath` is version-independent (`requirements/REQ-001-unified-inbound-inbox.md` not `requirements/REQ-001.../v38`). Every new approved version replaces the previous version's chunks. The help chat always answers from the latest approved version of each artefact.

---

## Retrieval Quality Guidelines

### What Works Well

**Content-focused queries:** "what is a CHECK in a REQ file?" → matches training content and pipeline docs directly

**Project-specific queries when inside a project:** "what hazards were identified for the notification feature?" → matches DCB0129 chunks for this project

**Concept queries:** "how do I handle a GAP response?" → matches Module 1 Exercise 2 content via semantic similarity

**Stage queries:** "what does P06 produce?" → matches pipeline prompt and training module content

### What Works Less Well

**Structural references:** "exercise 2 of Module 1" — the breadcrumb prefixing improves this significantly but content-focused queries always perform better

**Very broad queries:** "tell me everything about the project" — too broad to retrieve focused chunks; ask about specific aspects instead

**Implementation details not in approved artefacts:** "what is the DB schema for the notification table?" — only indexed if this appears in an approved ARCH artefact for this project

### Diagnostics

To check what is indexed for a namespace:

```sql
SELECT source_path, COUNT(*) as chunks
FROM knowledge_document
WHERE namespace = 'genesis_tool'
GROUP BY source_path
ORDER BY chunks DESC;
```

To check project artefact indexing:

```sql
SELECT source_path, COUNT(*) as chunks
FROM knowledge_document
WHERE namespace = 'project_artefact'
AND project_id = '{your-project-id}'
GROUP BY source_path
ORDER BY source_path;
```

---

## Workstream C Plug-In Point

`HelpChatStreamService` calls `IKnowledgeService.QueryAsync`. This interface is the seam for the Context Graph.

When Plan KG (Workstream C) is ready:
- `IKnowledgeService` is swapped for a Knowledge Graph MCP call
- No changes to `HelpChatController`, `HelpChatStreamService`, or the frontend
- The help chat gains: cross-project patterns, blast radius analysis, codebase context, migration status
- One DI registration change — the entire retrieval layer upgrades transparently

---

## Constants Reference

| Constant | Value | File |
|----------|-------|------|
| `TargetWordCount` | 150 | `BedrockKnowledgeService.cs` |
| `HardCapChars` | 6000 | `BedrockKnowledgeService.cs` |
| `MaxTopN` | 20 | `BedrockKnowledgeService.cs` |
| `OverlapWordTarget` | 30 | `BedrockKnowledgeService.cs` |
| `GenesisTool topN` | 3 | `HelpChatStreamService.cs` |
| `ProjectArtefact topN` | 5 | `HelpChatStreamService.cs` |
| Embedding dimensions | 1024 | `BedrockEmbeddingService.cs` |
| Embedding model | `amazon.titan-embed-text-v2:0` | `BedrockEmbeddingService.cs` |

---

*Genesis AI Knowledge Service — RAG Rules v1.0 | July 2026*
*Next update: when Plan KG (Context Graph) replaces IKnowledgeService*
