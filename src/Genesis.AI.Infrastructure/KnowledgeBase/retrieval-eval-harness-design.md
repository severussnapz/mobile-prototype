# Retrieval Evaluation Harness — Design

**Purpose:** Define the eval-set schema, generation pipeline, scoring model, and harness architecture for measuring retrieval quality of the Genesis AI Knowledge Service — before any chunking change lands. This document is for sign-off. No implementation before sign-off.

**Companion prompts:**
- `eval-query-generator.md` — generates candidate queries from artefact sections
- `eval-query-judge.md` — filters candidates on question quality (never on retrievability)

**Scope:** `project-artefact` and `genesis-tool` namespaces of `IKnowledgeService`. Out of scope: Workstream C graph retrieval (the anchor model is designed to extend to it, but graph traversal scoring is not specified here).

---

## 1. Design Decisions

### D1 — Chunk-independent ground truth (load-bearing)

The harness exists to compare chunkers. Ground truth therefore never references chunk IDs — chunks change with every chunker. Expectations are anchored at the **artefact level**:

- `sourcePath` — the artefact file
- `anchor` — a location within it, resolved at scoring time

Everything else in this design depends on D1. If ground truth were chunk-addressed, the eval set would need rebuilding on every chunker change, and cross-chunker comparison would be meaningless.

**Anchor types:**

| Type | Anchor value | Resolved span |
|---|---|---|
| `headingPath` | e.g. `HAZ-DOC-002 > Mitigation` | Content of that heading section (heading line to next same-or-higher heading) |
| `identifier` | e.g. `REQ-042` | Smallest enclosing heading section containing the first occurrence of the ID |

Resolution is performed by a harness-side Markdown heading parser (`IAnchorResolver`) against the source document — independent of any chunker.

### D2 — Chunker offset contract (prerequisite change)

To score "does a retrieved chunk cover the anchored span", every chunk must record its **character offset range** in the source document.

Required change: the chunking step inside `BedrockKnowledgeService.IndexDocumentAsync` must emit `(content, charStart, charEnd)` per chunk, and the offsets must be stored in chunk `metadata` (existing JSONB column — no migration). This is a small, contained change to the current fixed splitter and a contract on every future chunker.

**Assumption flagged:** this is the one code change that must land *before* the baseline run. It touches indexing only; `QueryAsync` is unchanged.

### D3 — Scoring model

A query is a **hit at k** if any of the top-k retrieved chunks satisfies both:
1. `chunk.sourcePath == expected.sourcePath` (and `projectId` matches where applicable)
2. `[chunk.charStart, chunk.charEnd]` overlaps the resolved anchor span by ≥ 1 character

Overlap-by-one is deliberate: a chunk that clips the section boundary still gives the LLM a foothold, and stricter overlap thresholds are a tunable refinement, not a day-one requirement.

**Negative queries** (corpus cannot answer): pass if the top-1 similarity score is below threshold `T`. **Caveat, stated per design-integrity standard:** `T` is retriever-dependent and unmeasured — it will be set empirically from the baseline run's score distribution, and negative-query results are reported separately, never blended into recall, because they are sensitive to this configuration in a way positive queries are not.

### D4 — Metrics and reporting

Per run, per `queryType`:
- **recall@5**, **recall@10**, **MRR**
- Negative pass rate (separate table, per D3 caveat)

The per-type split is the diagnostic signal:
- `identifier` failures → lexical matching gap (motivates hybrid tsvector + vector)
- `paraphrase` failures → chunk context loss (motivates structure-aware / contextual chunking)
- `situated` failures → query-register mismatch (prompt/injection problem, not chunking)

An aggregate number is reported last and never used for decisions on its own.

**Run report** (one Markdown file per run, committed):
- Chunker identifier + configuration
- Corpus snapshot hash (D6)
- Eval set version
- Metrics tables per queryType
- Full failed-query list (queryId, query, expected anchor, top-3 retrieved with scores) — this list is the debugging corpus for the next iteration

### D5 — Baseline rule

The first run is against the **current fixed 512/64 splitter** and its report is committed as the reference before any chunker change lands. Every subsequent report states its delta against baseline. No chunker change merges without a run report.

### D6 — Corpus pinning

Eval runs execute against a **pinned corpus snapshot** — a directory of artefact files with a content hash recorded in the report. Otherwise metric deltas conflate corpus drift with chunker change. The snapshot is refreshed deliberately (new snapshot = new baseline run), never implicitly.

### D7 — Governance

- Eval set is JSONL, committed to the repo, changed via PR — same governance as prompt files (CODEOWNERS).
- Every record carries `provenance` and `status`. Only `status: accepted` records are scored.
- **Retrievability non-filter rule (hard rule):** no generated query is ever filtered, rejected, or down-weighted because the current retriever fails to find its source. The judge filters on question quality only. Filtering on retrievability selects for queries the current system already passes and silently inflates every baseline.

### D8 — What this eval set is, and is not

With a **sampled human review** (D9, Stage 4) the set is sufficient to compare chunkers — relative deltas are the job. It is **not** a golden dataset: absolute recall numbers are indicative only until real Help Chat queries are mined in (`provenance: real`), per the standing "corrections are the eval corpus" principle. Known synthetic-eval bias: it tests retrieval of the passage that generated the question, and its query distribution differs from real users'. The paraphrase constraint and situated register mitigate this; they do not eliminate it.

---

## 2. Eval Record Schema (JSONL, one record per line)

```json
{
  "queryId": "EV-0042",
  "query": "What mitigation was agreed for stale document locks?",
  "queryType": "paraphrase",
  "namespace": "project-artefact",
  "projectId": "b7e2…",
  "expected": {
    "sourcePath": "hazards/HAZ-LOG.md",
    "anchorType": "headingPath",
    "anchor": "HAZ-DOC-002 > Mitigation"
  },
  "provenance": "generated",
  "status": "accepted",
  "generator": { "model": "…", "promptVersion": "eval-query-generator v1" },
  "judge": { "verdict": "accept", "promptVersion": "eval-query-judge v1" },
  "createdAt": "2026-07-11T00:00:00Z"
}
```

Field rules:
- `queryType`: `identifier | factual | paraphrase | situated | negative`
- `expected`: `null` when `queryType` is `negative`; required otherwise
- `provenance`: `deterministic | generated | real` — `deterministic` for Stage 1 template queries, `generated` for LLM output, `real` for mined production queries (future)
- `status`: `candidate | accepted | rejected` — Stage 1 records are born `accepted`; Stage 2 records are born `candidate` and promoted by judge + human sample
- `generator` / `judge` blocks are audit trail; absent on `deterministic` records

---

## 3. Generation Pipeline

### Stage 1 — Deterministic identifier queries (zero LLM cost)

Regex sweep over the pinned corpus for ID patterns (`REQ-\d+`, `HAZ-[A-Z]+-\d+`, `ADR-\w+`, `GENESIS-\d{3}`, …). Template-instantiate queries per ID class:
- "What is the mitigation for {HAZ-ID}?"
- "What are the acceptance criteria for {REQ-ID}?"
- "What was decided in {ADR-ID}?"

Anchor: `identifier` type, resolved to the enclosing section. Born `accepted` — templates are unambiguous by construction. Expected yield: 30–40% of the set for zero cost (proportion is an estimate, not a measurement — actual yield depends on ID density in the pinned corpus).

### Stage 2 — LLM-generated natural queries

For each heading section of each artefact in the snapshot: one Bedrock call (temperature 0.5 per ADR-010 exploratory setting) using `eval-query-generator.md`. Produces 2–3 questions with forced type diversity (`factual`, `paraphrase`, `situated`). Prompt-cache the artefact, vary the section — this is a background indexing-style cost, not a query-path cost.

### Stage 3 — LLM judge filter

Each candidate through `eval-query-judge.md` (temperature 0 per ADR-010 deterministic setting). Reject reasons: `NOT_SELF_CONTAINED`, `NOT_ANSWERABLE`, `AMBIGUOUS_WITHIN_ARTEFACT`, `TRIVIAL_LEXICAL_COPY`, `MALFORMED`. Meaningful rejection volume is the filter working, not a defect.

**Honest limitation:** the judge receives the source artefact's heading tree, so its ambiguity check is **intra-artefact only**. Cross-artefact ambiguity (two artefacts answering the same question) is not machine-checked in v1; it surfaces in the failed-query list during runs and in the human sample. Accepted as a v1 gap.

### Stage 4 — Human ratification (sample, not census)

A pre-populated accept/reject list. Deterministic records: no review needed. Generated records: review a sample (size to be agreed at sign-off — this is an open decision, §6). Rejections feed back as generator/judge prompt improvements.

### Stage 5 — Negative queries

A small hand-count of questions the corpus cannot answer (authored during the Stage 4 review sitting — the one place a few minutes of writing is unavoidable, kept to single digits). Scored per D3.

---

## 4. Harness Architecture

Smallest thing that works, inside the existing stack:

- **Location:** xUnit project `Genesis.AI.RetrievalEvals` (or an `[Trait("Category","Eval")]` set inside IntegrationTests — open decision, §6). Excluded from the default CI test run; executed on demand and on chunker-change PRs.
- **Fixture:** Testcontainers Postgres + pgvector, existing fixture pattern (`TESTCONTAINERS_RYUK_DISABLED=true` first line of `InitializeAsync`, Colima `DOCKER_HOST`).
- **Flow per run:**
  1. Load pinned corpus snapshot; compute hash
  2. Index every artefact through `IKnowledgeService.IndexDocumentAsync` with the chunker-under-test (offset contract per D2)
  3. Load eval set (accepted records only)
  4. For each query: `QueryAsync(query, namespace, projectId, topN: 10)`
  5. Score per D3 via `IAnchorResolver`
  6. Emit run report (D4)
- **Key interfaces:**

```csharp
public interface IAnchorResolver
{
    CharSpan Resolve(EvalAnchor anchor, string documentContent);
}

public record EvalAnchor(string SourcePath, EvalAnchorType Type, string Anchor);
public record CharSpan(int Start, int End);
```

- **Dependencies flagged:** runs require Bedrock access (query embedding via Titan v2). Local runs need VPC/credential reach; if that is a friction point, a deterministic local embedding stub is explicitly **not** acceptable for scored runs (it would measure the stub, not the retriever) — runs happen where Bedrock is reachable, full stop.

---

## 5. Cost and Effort (estimates, not measurements)

- Generation: one pass over the corpus ≈ one Bedrock call per section (generator) + one per candidate (judge). Order of a few hundred calls for the current artefact volume; background, cacheable.
- Human input: one sampling-review sitting plus authoring single-digit negative queries.
- Code: D2 offset change, anchor resolver, runner, report writer, two prompt files, Stage 1 template sweep. No new infrastructure, no migration.

---

## 6. Open Decisions for Sign-off

1. **Eval project shape** — separate `Genesis.AI.RetrievalEvals` project vs `Category=Eval` trait in IntegrationTests. Recommendation: separate project (keeps eval-only dependencies and the report writer out of the test tree).
2. **Sample size for Stage 4** — proposal: 100% of `paraphrase` (highest-risk type), fixed sample of `factual`/`situated`. Exact number is yours to set.
3. **Negative threshold `T` policy** — set from baseline score distribution (recommended) vs fixed a priori.
4. **Corpus snapshot source** — export of currently approved artefacts from a chosen project vs a curated fixture corpus. Recommendation: real approved artefacts, exported once and pinned.
5. **Eval run trigger** — manual only vs required on chunker-change PRs. Recommendation: required on any PR touching the chunking path, manual otherwise.

---

## 7. Implementation Checklist (post sign-off, TDD throughout)

- [ ] D2: chunker emits char offsets into chunk metadata (RED: offset assertions on existing splitter → GREEN)
- [ ] `IAnchorResolver` + Markdown heading parser (RED: headingPath and identifier resolution cases → GREEN)
- [ ] Stage 1 template sweep tool + first deterministic records committed
- [ ] Generator + judge prompts committed to KnowledgeBase; generation run executed; candidates committed
- [ ] Stage 4 review sitting; accepted set committed as eval-set v1
- [ ] Runner + report writer (RED: scoring cases incl. boundary overlap, wrong-source, negative → GREEN)
- [ ] Baseline run against fixed 512/64 splitter; report committed as reference
- [ ] PR gate rule: chunking-path changes require an attached run report

---

## 8. Two Namespaces (v1.1 addendum)

The Knowledge Service has two corpora with different content, refresh mechanics, and query registers. The harness treats them as first-class, not as one blended corpus.

### Two pinned corpora, not one

- **`genesis-tool`** — snapshot is the embedded Markdown resources in `Genesis.AI.Infrastructure` at a given commit. Its hash is deterministic from the repo; no export step.
- **`project-artefact`** — snapshot is an export of approved artefacts from a nominated project (§6 decision 4), pinned per D6.

Each run report states both corpus hashes.

### Generation differs per namespace

- **Stage 1 identifier sweep** uses different pattern sets: `REQ-/HAZ-/ADR-/GENESIS-` for project artefacts; pipeline stage IDs (`P01`–`P11`) and tool names (`save_artefact`, `edit_artefact`, …) for tool docs.
- **Stage 2 type weighting:** tool docs are how-to content — `situated` queries dominate ("how do I resolve a parking lot item?"), `paraphrase` still applies, `factual` reduced. Project artefacts get the standard three-type spread. The generator prompt is unchanged; the weighting is pipeline configuration (which slots are requested per section).
- Every record already carries `namespace` and `projectId` (§2 schema) — no schema change.

### Scoring runs in three modes

1. **Tool-only** — queries with `namespace: genesis-tool`, retrieval filtered to that namespace.
2. **Project-only** — likewise for `project-artefact` + `projectId`.
3. **Combined (Help Chat path)** — the same queries run the way `HelpChatStreamService` actually queries when a `projectId` is present: both namespaces together. This mode measures **cross-namespace interference** — recall@k delta versus the query's single-namespace mode. A tool-doc chunk crowding the correct REQ section out of the top-k (or vice versa) is a real production failure mode that single-namespace evals cannot see, and it is reported explicitly.

Metrics (D4) are reported per namespace per queryType; interference deltas per queryType in a separate table.

### Chunker-change staleness (cross-reference)

A chunker change must reach both namespaces or eval results diverge from production reality: the `genesis-tool` seeder's idempotency hash must include the chunker version, and unamended project artefacts need a re-index pass. Mechanics specified in `structure-aware-chunker-design.md` §C6; recorded here because a harness run against a freshly-indexed test container will silently disagree with a production estate still carrying old-chunker chunks if C6 is not implemented.

---

*Document owner: Idris Issa | Version: 1.1 — for sign-off | Classification: Internal*
