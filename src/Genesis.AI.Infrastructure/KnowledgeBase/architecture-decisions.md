# Genesis AI — Architecture Decisions (ADRs)

## ADR-001: AngleSharp over Graph-based DOM
**Date:** June 2026
**Status:** Implemented

**Decision:** Replace custom graph JSON (1,028 nodes) with AngleSharp DOM parsing for prototype editing.

**Context:** Graph approach required custom graph JSON per prototype, had no parent/sibling context, every edit rebuilt the entire graph, no CSS selector support, brittle string replacement.

**Consequences:** Direct HTML parsing with CSS selectors, structure-aware editing, batch mutations in one parse cycle, one S3 assembly at the end.

---

## ADR-002: Numeric refs over node_ids for list_elements
**Date:** June 2026
**Status:** Implemented

**Decision:** list_elements returns [1], [2], [3] numeric refs not hex node_ids.

**Context:** Model cannot reliably copy hex hashes (e.g. `B15A7D2BB4308787`) without hallucination or truncation.

**Consequences:** Refs resolve server-side via `listElementsRefMap`. Model only ever sees and uses small integers.

---

## ADR-003: apply_to_scope as the correct bulk edit pattern
**Date:** June 2026
**Status:** PENDING implementation (Plan 3c)

**Decision:** Replace `list_elements + apply_bulk_attributes` with `apply_to_scope` for all bulk operations.

**Context:** LLM cannot maintain DOM order for bulk value assignment regardless of prompting strategy. Model generates values in semantic/functional order, causing offset mutations.

**Rationale:** Separating intent translation (LLM) from deterministic execution (API) is the correct architecture. The LLM describes the operation in structured terms. The API handles element discovery, value generation, mutation, and verification.

**Tool contract:**
```json
{
  "scope": "screen-gallery-file",
  "selector": "button",
  "operation": "set_attribute|add_class|remove_class|set_text|remove_attribute",
  "attribute": "aria-label",
  "strategy": "derive_from_text_content|literal|generate_from_context",
  "value": "optional for literal strategy"
}
```

---

## ADR-004: Fail-closed batch mutations
**Date:** June 2026
**Status:** Implemented

**Decision:** Any validation failure in apply_bulk_attributes rejects the entire batch. Partial success = failure.

**Context:** Partial writes produce wrong data silently. The model reports "Done" based on tool return values regardless of actual HTML state.

**Consequences:** `IsSuccessfulBulkApplyResult` validates full "Applied N of N" format only. Model cannot claim success without proof.

---

## ADR-005: Output Template Contracts before Code Swarm
**Date:** June 2026
**Status:** PENDING (Plan 3d)

**Decision:** Plan 3d (output template contracts) must complete before Plan 6 (code swarm).

**Context:** The swarm needs to know what "done" means per pipeline. Without a schema defining required sections and quality gates, the swarm has no completion criteria.

**Consequences:** Each pipeline has a defined template. TDD agent extracts tests from template sections. Swarm writes code until tests pass. Quality gate validates template completeness.

---

## ADR-006: Prototype chat as requirements feedback interface
**Date:** June 2026
**Status:** PENDING (Plan 3d)

**Decision:** The prototype conversation is the dual-mode interface for both UI editing and requirements feedback.

**Modes:**
- EDIT MODE: UI changes → apply_to_scope → prototype updated
- FEEDBACK MODE: Functional concerns → GAP | CLARIFICATION | CONTRADICTION → REQ-*.md updated → human approves → prototype updated

**Consequences:** Every change is auditable. CHANGE-{id}.md created for every requirement change. Nothing changes without human approval.

---

## ADR-007: Context graph is EMIS Web
**Date:** June 2026
**Status:** SUPERSEDED by ADR-011 (graph platform model) and ADR-012 (construction tooling). The two points below remain valid; the single-graph framing and flat source list are superseded.

**Decision:** The context graph is built from EMIS Web — not a generic knowledge graph but a formalisation of 25 years of EMIS clinical IP.

**Sources:** Roslyn AST, GitHub history, DB schema, DCB0129 hazard logs, API contracts, NHS integration definitions, architecture decisions.

**Specialist requirement (STILL VALID):** Knowledge graph extraction from heterogeneous legacy sources + LLM fine-tuning on domain graphs requires specialist partners. TPG to provide introductions. EMIS owns all output — the IP is ours, specialists provide extraction methodology and tooling.

**Superseded because:** the context graph is now a *platform of eight independent graphs* (ADR-011), not one graph; the sources are organised per-graph with distinct owners and certification tiers, not a flat list; and the repo graph construction tooling is now decided (ADR-012). See workstream-c-design.md for the full design.

---

## ADR-008: Dual storage architecture
**Date:** June 2026
**Status:** AGREED — not Genesis AI's responsibility to build

**Decision:** EMIS-X uses EMIS Web storage (existing, system of record) and cloud-native storage (new, for new data structures) in tandem. Abstraction layer routes between them and can combine data from both.

**Genesis AI relevance:** Context graph must model both storage layers so architecture generation is correct. Any generated code that routes data incorrectly is a patient safety risk.

---

## ADR-009: Genesis AI as programme multiplier not programme
**Date:** June 2026
**Status:** STRATEGIC PRINCIPLE

**Decision:** Genesis AI is not the EMIS Web → EMIS-X migration programme. It is the AI multiplier that makes the programme faster, safer, and more traceable.

**What other teams own:** EMIS-X FE development, abstraction layer, cloud-native storage, data migration strategy.

**What Genesis AI owns:** Making every one of those workstreams faster. Requirements in hours. Prototypes in minutes. Architecture by exception. Safety by exception. Code against pre-generated tests.

---

## ADR-010: GitHub Contents API as the concurrency lock for artefact pushes
**Date:** July 2026
**Status:** Implemented (Plan 4c)

**Decision:** No application-level locking is needed for concurrent artefact pushes to `.genesis/`. The GitHub Contents API SHA requirement is the concurrency control mechanism.

**Context:** Two engineers approving artefacts simultaneously could race to push to the same file path in the feature repo. The question was whether Genesis AI needs an additional locking mechanism (e.g. a DB-level advisory lock, a queue, or a distributed mutex) to prevent corruption.

**Rationale:** Every PUT to the GitHub Contents API requires the current blob SHA. If two pushes race:
1. Both resolve the current SHA via GET.
2. The first PUT succeeds and changes the blob SHA.
3. The second PUT returns HTTP 422 — the SHA it holds is now stale.
4. `GitHubContentsService` catches the 422, re-fetches the current SHA, and retries the PUT.

This is standard optimistic concurrency. The Contents API is the lock. No additional mechanism is needed. The retry is bounded by the Polly resilience pipeline (3 attempts, exponential backoff, max 30s).

**Consequences:**
- No DB advisory locks, no queues, no distributed mutexes in the push path.
- `GitHubContentsService.PushFileAsync` must handle HTTP 422 specifically — re-GET SHA, retry PUT — distinct from the general 429/5xx retry policy.
- Concurrent approvals of *different* files in the same repo are fully independent and never conflict.
- Concurrent approvals of the *same* file (rare — two people approving the same artefact simultaneously) converge correctly: one wins, the other retries with the updated SHA and pushes a new version on top.
- The `push_failure_log` captures any case where all retries are exhausted — audit trail preserved regardless of outcome.

---

## ADR-011: Context graph is a platform of independent graphs
**Date:** June 2026
**Status:** AGREED — design complete, build gated on Plans 3c/3d + TPG introductions
**Supersedes:** the single-graph framing of ADR-007

**Decision:** The context graph is not one graph. It is a platform of eight independent graphs, each with a single source of truth, a single owner, and a distinct query purpose. Graphs are independent at build time and connected at query time by the agent depending on the task.

**The eight graphs:**
1. Repo graphs — one per repo. Code structure, commit history, DB schema, complexity and impact signals.
2. Capability catalogue — derived from repo graphs, enriched with Confluence. What EMIS does as a product.
3. UI Kit graph — design system, components, CSS tokens, usage rules.
4. DCB0129 graph — clinical safety hazard logs, Excel-per-area, CSO owned.
5. Security graph — security controls and obligations.
6. IG graph — information governance obligations.
7. ServiceNow graph — support tickets and resolution patterns, mapped to namespaces.
8. Manuals and guides graph — operational knowledge.

**Ground truth principle:** The graph is not a suggestion engine. It is the ground truth layer that constrains what the LLM can assert. For anything with a more primary source, the graph is a queryable *projection* of that source (the code is ground truth; the repo graph projects it) — when they disagree, re-derive the graph, never patch it. Agents are read-only to all graphs; every enrichment comes through a certified path (merged PR, approved artefact, human sign-off).

**Tiered trust model:**
- Tier 1 (DCB0129, Security, IG) — human certified, hard constraints. The graph is floor and ceiling.
- Tier 2 (repo, capability, ServiceNow, manuals) — ground and extend. The graph is the floor; the agent may reason beyond it where there is no signal, flagging explicitly.
- Tier 3 (null/low-confidence nodes) — signal only, surfaced with a warning.

**Decision provenance:** Every agent output carries the graph node IDs that grounded it, snapshotted at generation time, accumulated across P01–P11, and written into the `Genesis-Graph-Nodes` commit trailer. This is distinct from any construction tool's edge-derivation tags. Provenance is what makes drift detection, self-correction, and regulated auditability possible.

**Consequences:** merge decisions between graphs are deferred until data shapes are known. Coverage is measured per namespace so "not clinical" is distinguishable from "not indexed." An evaluation benchmark (Stage 0) proves value before scale. Full design, roadmap, and worked example in workstream-c-design.md.

---

## ADR-012: Roslyn + Neo4j for .NET repo graphs; Graphify for docs only
**Date:** June 2026
**Status:** Accepted — team-validated against EMIS Web
**Supersedes:** the tooling implications of ADR-007. Canonical record of ADR-C2 in workstream-c-design.md.

**Decision:** Roslyn + Neo4j is the repo graph construction stack for the .NET estate. Graphify is retained only for non-.NET document and multi-modal graphs (manuals, guides, Confluence enrichment).

**Context:** The team tested graph construction options against EMIS Web. Graphify performed poorly on the .NET codebase. Root cause is architectural: Graphify's C# support is tree-sitter (syntactic structure only), not semantic. Roslyn compiles the solution and resolves the full semantic model — symbols, overloads, generics, interface implementations, cross-project references — which is what a codebase of EMIS Web's age and size requires.

**Rationale:** semantic resolution is not optional for a 25-year .NET estate. The spike proved tree-sitter insufficient empirically. Neo4j is the graph store with a Cypher query surface; EMIS-specific enrichment layers on top of the Roslyn-extracted graph.

**Consequences:**
- Known Neo4j-at-scale work: default query/row limit overrides for EMIS Web scale, index strategy on most-traversed labels, pagination, variable-length path bounding.
- Non-Roslyn legacy code (VB6, etc.) is a coverage gap to be scoped, not assumed covered.
- The MCP serving surface over Neo4j is our build — it must enforce Tier 1 constraints, attach confidence/coverage metadata, and emit node IDs for decision provenance (ADR-011). Not delegated to a generic graph-serving tool.
- Open integration points (Neo4j deployment/sovereign boundary, Roslyn extraction schema stability, legacy coverage, incremental update cadence) to be closed in the Idris + Luke fit-together session.

**Note:** ADR-C1 in workstream-c-design.md (adopt Graphify as the Tier 2 construction layer) was superseded by this decision before any build. The spike caught the problem — the Stage 0 gate working as designed.
