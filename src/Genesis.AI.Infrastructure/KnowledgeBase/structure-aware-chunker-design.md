# Structure-Aware Chunker — Implementation Design

**Purpose:** Replace the fixed 512/64 splitter inside `BedrockKnowledgeService` with a structure-aware Markdown chunker, behind a new `IChunker` seam. Covers interface, chunk record shape, algorithm, namespace/reseed handling, and TDD plan. For sign-off — no implementation before sign-off, and no merge without an eval run report against the committed baseline (see `retrieval-eval-harness-design.md` §D5).

**Scope:** chunking only. Small-to-big retrieval (`parent_id`), contextual prefixes, and hybrid lexical search are separate follow-on designs. This chunker is designed so those follow-ons need no re-chunking (C8).

---

## 1. Design Decisions

### C1 — Extract `IChunker` as a seam

Chunking currently lives inline in `BedrockKnowledgeService.IndexDocumentAsync`. Extract it:

```csharp
public interface IChunker
{
    /// Semantic version of the chunking algorithm + configuration.
    /// Changing algorithm or defaults REQUIRES bumping this.
    string Version { get; }

    IReadOnlyList<Chunk> Chunk(string content, ChunkingOptions options);
}

public sealed record Chunk(
    string EmbeddingText,     // what is embedded (heading-path prefix + content)
    string StoredContent,     // what is stored and returned (verbatim source text)
    int CharStart,            // offset of StoredContent in source document
    int CharEnd,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record ChunkingOptions(
    int TargetTokenBudget = 512,   // soft ceiling — tunable, validated by harness
    int MinMergeTokens = 128);     // sections below this merge with siblings — tunable
```

Registration: `FixedSizeChunker` (current behaviour, retrofitted with offsets) and `StructureAwareMarkdownChunker` both implement `IChunker`. The eval harness takes any `IChunker`; production DI registers one. This is the mechanism that makes chunkers comparable and satisfies the harness's D2 offset contract in one move.

**Ponytail check applied:** the seam exists because two real implementations must be compared by the harness — it is not a speculative abstraction.

### C2 — Embedding text vs stored content (regulated fidelity)

- `EmbeddingText` = heading path prefix + section content. The prefix (`"REQ-042 > Acceptance Criteria\n\n…"`) disambiguates the embedding.
- `StoredContent` = **verbatim source text, unmodified.** The retrieval layer must never return paraphrased or decorated versions of ratified artefact content — the same reason propositional chunking was ruled out. The heading path is available in metadata; injection assembly may display it, but the stored chunk is the artefact's own words.

### C3 — Algorithm

1. **Parse** the document into a heading tree (ATX `#`–`######`). Fenced code blocks (```` ``` ````/`~~~`) are opaque: `#` inside a fence is content, never a heading. Tables are atomic blocks.
2. **Preamble** before the first heading becomes its own chunk; heading path = the document title (first H1 if present, else `sourcePath` filename).
3. **Leaf sections** are candidate chunks, with char spans `[headingLineStart, nextSameOrHigherHeading)`.
4. **Merge** adjacent sibling sections under the same parent while combined size < `MinMergeTokens`, up to `TargetTokenBudget`. Merged chunk's heading path = the shared parent path; individual child paths recorded in metadata.
5. **Split** sections exceeding `TargetTokenBudget` at paragraph boundaries (blank lines), emitting `part 1/n` metadata. **Never split inside a fenced code block or table** — an atomic block exceeding the budget stays whole and is flagged `oversize=true`. The hard ceiling is the embedding model input limit (Titan v2, 8k tokens), not the soft budget.
6. **Fallback ladder** for structure-poor input: no headings → paragraph-boundary splitting at the token budget; no paragraphs (single blob) → fixed-size with overlap, offsets still emitted. Structure-aware degrades gracefully; it never refuses input.

### C4 — Token counting is approximate (stated, not hidden)

No exact Titan v2 tokenizer is available in-process. Budget enforcement uses a character-based approximation (`chars / 4`). Consequence: budgets are soft and the constant is a heuristic — acceptable because the only hard limit (model input size) has wide headroom, and chunk-size effects are exactly what the eval harness measures. Do not present the budget numbers as validated; they are defaults to be tuned against recall@k.

### C5 — Chunk metadata (per chunk)

Existing fields preserved: `artefact_type`, `stage`, `project_id`, `source_path`, `version`. Added:

| Key | Value |
|---|---|
| `heading_path` | `"HAZ-DOC-002 > Mitigation"` |
| `char_start` / `char_end` | offsets in source document (D2 contract) |
| `chunker_version` | `IChunker.Version` |
| `seq` | chunk ordinal within document |
| `part` / `oversize` | only when produced by step 5 |

JSONB `metadata` column absorbs all of this — **no schema migration**.

### C6 — Namespace handling: reseed and re-index on chunker change

The two namespaces have different refresh mechanics, and both have a latent staleness trap this design closes:

**`genesis-tool` (seeded from embedded resources):** `KnowledgeSeederService` is idempotent on a content hash — as currently designed, a chunker change with unchanged content would **skip reseeding, leaving stale chunks indexed under the old chunker**. Fix: the idempotency hash becomes `hash(content + IChunker.Version)`. Chunker bump → hash differs → reseed on deployment. One-line concept, and it is load-bearing: without it the tool namespace silently never adopts a new chunker.

**`project-artefact` (indexed at approval):** artefacts are re-indexed on amendment, but an unamended artefact would keep old-chunker chunks indefinitely. Fix: a re-index pass on startup (hosted service) that selects `DISTINCT (namespace, project_id, source_path)` where any chunk's `chunker_version != current`, and re-indexes each from the stored artefact content via the existing delete-then-insert path. Idempotent, resumable, background — indexing cost only, no query-path impact. (Alternative — an admin-triggered re-index endpoint — is listed as an open decision, §5.)

### C7 — What does not change

`IKnowledgeService` interface, `QueryAsync`, the `knowledge_documents` table, the Help Chat, and all callers: unchanged. The change surface is `IndexDocumentAsync` internals + DI + seeder hash + re-index hosted service.

### C8 — Forward compatibility (explicitly out of scope, designed for)

- **Small-to-big:** `heading_path` + `seq` + offsets are sufficient to resolve a chunk's parent section from the source document at injection time, so `parent_id` can be added later **without re-chunking**.
- **Contextual prefixes:** would extend `EmbeddingText` assembly only; `StoredContent` fidelity rule (C2) already protects the regulated boundary.

---

## 2. Sequencing (interlocks with the eval harness)

1. Extract `IChunker`; retrofit `FixedSizeChunker` with offsets (harness D2 prerequisite)
2. **Baseline eval run on `FixedSizeChunker` — report committed** (harness D5)
3. Implement `StructureAwareMarkdownChunker` (TDD, §3)
4. Eval run; compare per-queryType deltas against baseline
5. Only on a favourable report: DI swap + seeder hash change + re-index service, single PR
6. Version bump of `IChunker.Version` is part of that PR — never separable

Steps 1–2 are shared groundwork with the harness; step 5 is the only production-behaviour change and it ships with its evidence attached.

---

## 3. TDD Plan — RED cases first (per standing TDD hard rule)

**Parser:**
- ATX headings H1–H6 build correct tree with correct char spans
- `#` inside fenced code block is not a heading (``` and ~~~ fences, nested fence content)
- Unclosed fence at EOF treated as fenced to EOF (no crash, no false headings)
- Preamble before first heading captured with document-title path
- Setext headings (`===`/`---` underlines): decide accept-or-ignore at sign-off (§5) — test pins the decision either way

**Chunking:**
- Leaf section → one chunk; `StoredContent` byte-identical to source slice at `[CharStart, CharEnd)`
- Small siblings merge under parent path; merged metadata lists child paths
- Oversized section splits at paragraph boundaries with `part` metadata; offsets remain correct per part
- Oversized atomic code block stays whole, `oversize=true`
- Heading-less document falls back to paragraph splitting; blob falls back to fixed-size; offsets emitted in all fallback modes
- `EmbeddingText` = path prefix + content; `StoredContent` contains no prefix (C2 fidelity assertion)

**Seeder / re-index:**
- Seeder re-seeds when chunker version changes, content unchanged
- Seeder skips when both unchanged
- Re-index service re-indexes only documents with stale `chunker_version`; second run is a no-op

**Offsets retrofit (FixedSizeChunker):**
- Every chunk's `[CharStart, CharEnd)` slice of source equals `StoredContent` (overlap regions included)

Copilot prompts split per standing rule: Prompt 1 tests only (RED expected), Prompt 2 implementation (GREEN expected); audit for the five anti-shortcut rules before commit — in particular no optional `IChunker?` with a fallback null object (GENESIS-001 class), the dependency is required.

---

## 4. Cost / Effort (estimates)

Pure in-process code — no new infrastructure, no migration, no new dependencies (heading parser is a small hand-rolled scanner; per the ponytail ladder, no Markdown library is warranted for ATX headings + fences). Re-index pass cost = one embedding call per chunk per stale document, background.

---

## 5. Open Decisions for Sign-off

1. **Setext headings** — accept (`===`/`---`) or ignore. Recommendation: ignore in v1 (your artefacts are ATX-only by convention; `---` doubles as horizontal rule and is a false-positive source). A pinned test documents the choice.
2. **Re-index trigger** — startup hosted service (recommended: zero-touch) vs admin endpoint (explicit control).
3. **Budget defaults** — 512 target / 128 min-merge proposed as starting points only; first tuning pass driven by baseline-vs-new eval deltas, not by convention.
4. **Merged-chunk anchor scoring interaction** — when siblings merge, a chunk may cover several sections; the harness's overlap scoring (D3) handles this natively, but confirm you're content that a merged chunk counts as a hit for any of its child sections. (It should — the injected text contains the answer.)

---

*Document owner: Idris Issa | Version: 1.0 — for sign-off | Classification: Internal*
