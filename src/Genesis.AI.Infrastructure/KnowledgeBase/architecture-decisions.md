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
**Status:** PENDING (Plan C)

**Decision:** The context graph is built from EMIS Web — not a generic knowledge graph but a formalisation of 30 years of EMIS clinical IP.

**Sources:** Roslyn AST, GitHub history, DB schema, DCB0129 hazard logs, API contracts, NHS integration definitions, architecture decisions.

**Specialist requirement:** Knowledge graph extraction from heterogeneous legacy sources + LLM fine-tuning on domain graphs requires specialist partners. TPG to provide introductions. EMIS owns all output — the IP is ours, specialists provide extraction methodology and tooling.

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
