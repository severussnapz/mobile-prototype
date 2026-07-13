# Knowledge Layer Improvement — Implementation Plan

**Purpose:** Single ordered plan taking the Knowledge Service from its current state to the target state, consolidating the four signed designs: `retrieval-eval-harness-design.md` (v1.1), `structure-aware-chunker-design.md`, `indexing-pipeline-design.md`, and the generator/judge prompt documents. Every phase has RED-first tests, a verify step, and — from Phase 3 onwards — an eval-report gate. No phase starts before the previous phase's gate is met.

---

## Current state → Target state

| | Current | Target |
|---|---|---|
| Chunking | Fixed 512/64, inline in `BedrockKnowledgeService` | `IChunker` seam; structure-aware Markdown chunker, adoption evidenced by eval report |
| Retrieval quality | Unmeasured | Eval harness, committed eval set, committed baseline, per-queryType metrics per namespace, three scoring modes |
| Staleness on change | Content-hash only — chunker/model changes silently ignored | Indexing fingerprint (chunker + contextualiser prompt + embedding model) drives seeder reseed and project-artefact sweep |
| Persistence | Delete-then-insert, transaction boundary unstated | Repository with atomic replace; assemble-fully-then-persist rule |
| Retrieval granularity | Chunk = injection unit | Small-to-big: match small, inject parent section |
| Embedding context | Bare chunk text | Heading-path prefix (all); contextual prefix (policy-selected artefacts) |
| Lexical matching | None — pure cosine | Hybrid tsvector + vector with rank fusion |

**Standing rules for every phase:** TDD hard rule (two Copilot prompts — tests RED first, then implementation GREEN); anti-shortcut audit before every commit (no optional deps/null objects, no warning suppression, no assertion edits, no build-config routing; grep the constructor); small staged commits; test counts verified after every prompt; conventional commits.

---

## Phase 0 — Sign-off (no code)

- [ ] Sign off the three design documents
- [ ] Resolve consolidated open decisions (§ Open Decisions Register below)
- [ ] Amend chunker design §C6 and eval design §8: `chunker_version` → `indexing_fingerprint` (P6 supersession)
- [ ] Decide plan placement: separate knowledge-layer track vs Plan 4d item (recommendation: **separate track** — nothing here is on the Plan 5 gate list, and Phases 1–3 can interleave with remaining Plan 4d work without contention; the only shared file surface is `BedrockKnowledgeService`)

**Gate:** all open decisions have an answer recorded in the design docs.

---

## Phase 1 — Seams, offsets, fingerprint (groundwork; no retrieval behaviour change)

**1.1 Extract `IChunker`; retrofit `FixedSizeChunker` with offsets**
- RED: every chunk's `[CharStart, CharEnd)` slice of source equals `StoredContent`, including overlap regions; existing chunk content byte-identical to pre-refactor output (characterisation test — this is a refactor, not a behaviour change)
- GREEN: extraction + retrofit

**1.2 Extract `IEmbeddingClient`**
- RED: orchestration unit tests against a mock embedder (batched call shape, order preservation); `ModelId` surfaced
- GREEN: extraction wrapping the existing Bedrock Titan call, Polly retained

**1.3 Indexing fingerprint + staleness machinery**
- RED: seeder reseeds when fingerprint changes with content unchanged; seeder skips when both unchanged; sweep re-indexes only stale-fingerprint documents; second sweep run is a no-op; sweep sources content from the artefact store, not from chunks
- GREEN: `indexing_fingerprint` in chunk metadata (placeholder components for stages that don't exist yet); seeder hash = `hash(content + fingerprint)`; `StaleChunkReindexService` hosted service (separate class from the seeder — different responsibility)

**Verify:** build clean; full suites green; an indexed document's chunks are identical to before except new metadata keys. **No production DI change yet** — `FixedSizeChunker` remains registered.

**Gate:** characterisation tests prove zero retrieval behaviour change.

---

## Phase 2 — Eval set (parallelisable with Phase 1; no code dependency on it)

**2.1 Pin corpora** — `genesis-tool` = embedded resources at a commit (hash recorded); `project-artefact` = export of the nominated project's approved artefacts (Phase 0 decision), hash recorded.

**2.2 Stage 1 deterministic sweep** — identifier templates per namespace (`REQ/HAZ/ADR/GENESIS` vs `P01–P11`/tool names); records committed born-accepted.

**2.3 Commit prompts** — `eval-query-generator.md`, `eval-query-judge.md` to KnowledgeBase, CODEOWNERS-governed.

**2.4 Generation run** — generator (temp 0.5, doc cached) per section with per-namespace type weighting; judge pass (temp 0); candidates committed with audit blocks.

**2.5 Ratification sitting** — sample review per Phase 0 decision (proposed: 100% paraphrase, sampled rest); author the single-digit negative queries; accepted set committed as **eval-set v1**.

**Gate:** eval-set v1 committed via PR.

---

## Phase 3 — Harness + baseline (requires Phases 1 and 2)

**3.1 Anchor resolver**
- RED: `headingPath` resolution (nested, fenced-code immunity), `identifier` resolution to enclosing section, missing-anchor failure case
- GREEN: harness-side Markdown heading parser + `IAnchorResolver`

**3.2 Runner, scoring, report writer**
- RED: hit/miss on span overlap incl. one-character boundary; wrong-source rejection; negative threshold behaviour; MRR/recall arithmetic; report content assertions
- GREEN: eval project (separate `Genesis.AI.RetrievalEvals` per Phase 0 decision), Testcontainers pgvector fixture (Ryuk-disabled first line), three scoring modes (tool-only / project-only / combined-interference)

**3.3 Baseline run** — `FixedSizeChunker`, both corpora, three modes; negative threshold `T` set from the score distribution and recorded.

**Gate (hard):** **baseline report committed as reference.** From this point, no PR touching the chunking/indexing path merges without an attached run report. This gate has no exceptions — it is the evidence mechanism the whole plan stands on.

---

## Phase 4 — Structure-aware chunker

**4.1 Heading parser** — RED per chunker design §3: ATX tree + spans, fence immunity (``` and ~~~, unclosed at EOF), preamble, setext decision pinned.

**4.2 Chunker** — RED: leaf sections, sibling merge under parent path, paragraph split with `part`, atomic oversize blocks, fallback ladder (headings → paragraphs → fixed) with offsets in every mode, `EmbeddingText` prefix present / `StoredContent` prefix absent.

**4.3 Eval run** — structure-aware vs baseline; per-queryType, per-namespace deltas; interference deltas.

**Gate:** favourable report. Expected signature (hypothesis, to be confirmed not assumed): `paraphrase`/`factual` recall up; `identifier` roughly flat — identifier gains are Phase 7's job. If paraphrase recall does **not** improve, stop and diagnose before cutover — do not proceed on momentum.

**4.4 Production cutover — single PR:** DI swap to `StructureAwareMarkdownChunker` + `IChunker.Version` bump (never separable) + eval report attached. Deployment fires the seeder reseed and the stale sweep automatically via the fingerprint.

**Verify in environment:** tool namespace re-seeded (fingerprint on chunks = current); project artefacts swept; Help Chat smoke test both namespaces.

---

## Phase 5 — Small-to-big

**5.1 Migration** — `V__add_parent_id.sql` (nullable `parent_id UUID` + index).

**5.2 Repository extraction with atomic replace**
- RED: atomicity seam test — embedding failure on chunk N of M → repository never called, prior chunks queryable; replace is one transaction
- GREEN: `IKnowledgeDocumentRepository.ReplaceDocumentChunksAsync`; orchestrator adopts assemble-fully-then-persist

**5.3 Parent-first insert + injection expansion**
- RED: children reference parents; query hit on child returns parent content deduplicated; token-budget cap on expansion
- GREEN: insert ordering; expansion at injection assembly

**5.4 Eval run.** Interpretation note (recorded in the report): from this phase the harness scores what `QueryAsync` returns for injection — parents — because injected context is what the LLM sees; recall is expected to rise partly by construction of larger returned spans. Compare like-for-like on the same definition thereafter.

**Gate:** report committed; Help Chat answer-quality spot check on "what did we decide in P01?"-class queries.

---

## Phase 6 — Contextual prefixes

**6.1 Step-3 sign-offs** — contextualiser prompt document (temp 0, situating-sentence spec per indexing design §P4.4); model selection (Workstream F-adjacent; small manual quality check on real artefacts before adoption).

**6.2 `PolicyContextualiser`**
- RED: in-policy chunk gets prefix, out-of-policy gets null; policy matches the signed allowlist+floor; seam test — prefix present in embedded text, absent in `StoredContent`; generation failure fails the document run (no silent skip)
- GREEN: single implementation, **required** constructor dependency, always invoked. Copilot audit reference: indexing design §P4.1 — reject any null-object or optional-parameter shape on sight.

**6.3 Fingerprint gains the prompt version** → deployment reseeds/sweeps automatically.

**6.4 Eval run** vs Phase 5. **Gate:** report. If gains don't justify the per-chunk generation cost, the honest outcome is to not adopt — the policy constant makes partial adoption (fewer artefact types) a one-line PR.

---

## Phase 7 — Hybrid lexical + vector

**7.1 Migration** — tsvector generated column over chunk content + GIN index.

**7.2 Query fusion**
- RED: exact-identifier query (`HAZ-DOC-002`, `UpdateProjectGitHubResult`-class tokens) ranks the containing chunk top-k where cosine alone does not (fixture-constructed case); RRF arithmetic; namespace/projectId filters preserved in both legs
- GREEN: `QueryAsync` runs both legs in one round-trip, reciprocal rank fusion

**7.3 Eval run.** **Gate:** report — the specific expected signature is `identifier`-type recall up with other types not regressing. That is the entire justification for this phase; if identifier recall doesn't move, diagnose the tsvector configuration before accepting.

---

## Phase 8 — Close-out and standing rules

- [ ] Repo policy recorded: chunking/indexing-path PRs require an attached eval run report (the Phase 3 gate made permanent)
- [ ] Baseline refresh policy documented: new corpus snapshot = new baseline run, deliberate only
- [ ] Real-query mining rule active: Help Chat queries (especially rephrased/thumbed-down) periodically ratified into the eval set as `provenance: real` — the synthetic set is the cold-start, not the destination
- [ ] Master plan updated with final test counts and committed report links

---

## Dependency map

```
Phase 0 ─→ Phase 1 ─┐
     └──→ Phase 2 ──┴─→ Phase 3 (baseline) ─→ 4 ─→ 5 ─→ 6 ─→ 7 ─→ 8
```

Phases 1 and 2 run in parallel. Everything after Phase 3 is strictly sequential — each phase's eval run must be attributable to that phase's change alone; bundling phases destroys the attribution and with it the evidence.

## Effort (estimates, not commitments)

Phase 1: ~2–3 days. Phase 2: ~1–2 days machine time + one review sitting. Phase 3: ~2–3 days. Phase 4: ~3–4 days. Phase 5: ~2–3 days. Phase 6: ~2–3 days plus prompt sign-off. Phase 7: ~2 days. Serialised worst case ≈ 3 working weeks; Phases 1+2 parallelism and interleaving with Plan 4d work make calendar time your call.

## Open Decisions Register (consolidated for Phase 0)

| # | Decision | Source | Recommendation |
|---|---|---|---|
| 1 | Eval project shape | Harness §6.1 | Separate `Genesis.AI.RetrievalEvals` project |
| 2 | Stage 4 sample size | Harness §6.2 | 100% paraphrase, sampled factual/situated |
| 3 | Negative threshold policy | Harness §6.3 | Set from baseline distribution |
| 4 | Corpus snapshot source (nominate project) | Harness §6.4 | Real approved artefacts, exported and pinned |
| 5 | Eval run trigger | Harness §6.5 | Required on chunking-path PRs |
| 6 | Setext headings | Chunker §5.1 | Ignore in v1, pinned test |
| 7 | Re-index trigger | Chunker §5.2 | Startup hosted service |
| 8 | Budget defaults | Chunker §5.3 | 512/128 starting points, harness-tuned |
| 9 | Merged-chunk scoring | Chunker §5.4 | Merged chunk hits for any child section |
| 10 | Contextualisation policy shape + location | Indexing §8.1 | Type allowlist + length floor, PR-governed constant |
| 11 | Contextualiser model | Indexing §8.2 | Park to Phase 6; Haiku-class candidate |
| 12 | Fingerprint format | Indexing §8.3 | Readable concatenation |
| 13 | Generation parallelism | Indexing §8.4 | Sequential-with-cache first |
| 14 | Plan placement | This plan, Phase 0 | Separate knowledge-layer track |

---

*Document owner: Idris Issa | Version: 1.0 — for sign-off | Classification: Internal*
