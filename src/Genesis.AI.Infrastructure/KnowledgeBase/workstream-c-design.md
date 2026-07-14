# Workstream C — Context Graph: Design Decisions

**Status:** Not Started — blocked on Plans 3c and 3d completing, then TPG partner introductions.  
**Session date:** June 2026  
**Owner:** Idris Issa  
**Classification:** Internal — Confidential

---

## The Vision

The context graph is not a development tool. It is the operating system for the organisation.

Every function — engineering, support, product, bids, planning, onboarding, training, innovation — running from the same structured organisational intelligence. Agents acting on it, humans approving what matters, guardrails enforcing what can never be bypassed.

25 years of EMIS knowledge — every capability, every architectural decision, every clinical safety classification, every support resolution pattern — structured, queryable, and available to any agent or interface that needs it. The graph compounds instead of depreciating. Every ticket resolved, every PR merged, every requirement approved enriches it for the next query.

This is the moat. A competitor can copy the tooling. They cannot replicate 25 years of EMIS clinical IP, hazard history, and capability knowledge — all queryable in context.

---

## Reference Documents

Two documents inform the design and should be read alongside this:

- **EMIS_AMP_v3.0** — EMIS Web AI Migration Platform. Five indexers, five MCP tools, six-gate CI/CD pipeline, 10-week adoption roadmap. Contains ten open questions to be answered before v1.0 baseline.
- **EMIS_ContextGraph_v6.0** — Realistic delivery plan. Addresses six concerns from v5.0 review. Thin-layer spike-first approach. 16-week phased delivery. The working delivery plan for Workstream C.

---

## Architecture — Graph Platform

Each graph has a single source of truth, a single owner, and a distinct query purpose. Independent at build time. Connected at query time by the agent depending on what task it is executing. Merge decisions between graphs deferred until data shapes are known — do not combine prematurely.

### 1. Repo Graphs
One graph per code repository. Follows the natural ownership and deployment boundary.

**Sources:** Roslyn AST (C#), GitHub commit history, DB schema. Nightly batch ingestion.

**What it gives you:**
- Code structure — classes, methods, namespaces, dependencies
- Complexity signal — files that change frequently, broadly coupled modules, volatile areas
- Impact signal — co-change coupling history, empirical blast radius, not just static dependency analysis
- Author expertise map — who understands each module based on recent commit history
- Migration status — which modules are live in EMIS-X, in flight, or still legacy (auto-derived from API gateway traffic logs, not a manually maintained manifest)

**Tooling:** Roslyn + Neo4j as the repo graph construction stack (team-validated against EMIS Web, June 2026). Roslyn compiles the solution and resolves symbols, overloads, generics, interface implementations, and cross-project references — the semantic depth that tree-sitter tools cannot provide for a codebase of EMIS Web's age and size. Extracted graph loaded into Neo4j. EMIS-specific enrichment built on top: clinical namespace tagging, strangler fig classification, Designer file co-change tracking. Graphify was tested and rejected for .NET repos — see ADR-C2.

### 2. Capability Catalogue
Derived from the repo graphs. Built up incrementally as repos are indexed. Enriched with Confluence in Phase 2.

**Phase 1 — derived from repos:** What capabilities exist, where they are implemented, what they couple to.

**Phase 2 — enriched with Confluence:** Functional descriptions, design decisions, the "why" behind each capability. Confluence content maps onto capability entries the repo graphs identified — not the other way around. Repos define what is in the catalogue; Confluence enriches it.

**What it gives you:** A business-level map of what EMIS does as a product. Queryable by capability name, domain, migration status, clinical risk classification. The foundation for requirements by exception, architecture by exception, and bid intelligence.

### 3. UI Kit Graph
Design system as a queryable graph. Separate from the repo graphs and capability catalogue.

**Sources:** UI Kit component library, CSS tokens, usage rules, composition constraints, design patterns.

**What it gives you:** When the agent generates a prototype or makes a UI decision, it queries the UI Kit graph to ensure compliance with the design system. No hallucinated components, no CSS that violates the token system. Design by exception.

### 4. DCB0129 Tool and Graph
Clinical safety hazard logs ingested into a structured, queryable DB.

**Source:** Separate Excel documents per clinical safety area. CSO is single owner. Consistent structure across all files — one parser handles all.

**Ingestion:** Nightly batch import. Filename is the canonical area identifier — CSO controls the mapping by controlling filenames. Upserts into DB schema, one schema per clinical safety area.

**What it gives you:** Every hazard ever filed against every EMIS Web capability — hazard IDs, severity classifications, mitigations, audit trail linkages to code commits. Queryable before any requirement, architecture decision, or code change that touches a clinical namespace.

**Interim protection before this graph is live:** Static clinical namespace blocklist hardcoded into the LLM custom instruction from day one. Agent must flag any class matching the blocklist and require human clinical review. Never removed — remains as secondary control even after the DCB0129 graph is live.

### 5. Security Graph
Security controls, obligations, and constraints as a queryable graph.

**Owner:** Security team. **Source:** TBD — to be confirmed with security team before design starts.

**What it gives you:** Agent queries before any requirement, architecture, or code decision that has a security surface. Security by exception rather than security as an afterthought.

### 6. IG Graph
Information governance obligations as a queryable graph.

**Owner:** IG team. **Source:** TBD — to be confirmed with IG team before design starts.

**What it gives you:** Agent queries before any data processing decision. IG by exception. DPIA coverage known before requirements are written.

### 7. ServiceNow Graph
Support tickets and resolution patterns, mapped to code namespaces via the ServiceNow product taxonomy.

**Owner:** Engineering (map maintenance), Support operations (ticket ownership). **Source:** ServiceNow API. One-time mapping exercise: ServiceNow product hierarchy → code namespaces (JSON file, maintained by engineering, reviewed quarterly). Stale map entries — alert immediately, fix before next run.

**What it gives you:** Every support ticket ever raised mapped to the namespace it relates to. Resolution patterns queryable before investigation begins. Feeds the support-to-PR closed loop.

### 8. Manuals and Guides Graph
Operational knowledge — manuals, guides, runbooks — indexed and queryable.

**Owner:** Support operations / engineering. **Source:** Existing manuals and guides library.

**Tooling:** Graphify — genuine fit for this graph. Handles PDFs, Markdown, and diagrams natively via multi-modal extraction. This is document, not .NET code, so the tree-sitter limitation that ruled it out for repo graphs does not apply.

**What it gives you:** Agent can retrieve relevant manual sections when debugging or supporting. Internal chat interface can answer operational questions grounded in the actual documentation rather than hallucinating.

---

**On combining graphs:** Do not decide prematurely. Build each graph separately first. The right merge boundary will become obvious once the data shapes are known. Premature combination creates coupling that is hard to undo.

---

## Confirmed Data Sources

| Source | Status | Graph | Phase |
|---|---|---|---|
| Roslyn AST | ✅ Structured, ready | Repo graphs | 1 |
| GitHub history | ✅ Structured, ready | Repo graphs | 1 |
| DB schema | ✅ Structured, ready | Repo graphs | 2 |
| Confluence | 📋 Phase 2 enrichment | Capability catalogue | 2 |
| DCB0129 Excel logs | 📋 Excel, CSO owned | DCB0129 graph | 4 |
| NHS integration contracts | 📋 Requires formalisation | TBD | 4 |
| ServiceNow tickets | 📋 API access TBD | ServiceNow graph | Phase 1 scale |
| Manuals and guides | 📋 Graphify fit (docs) | Manuals graph | Phase 3 |

---

## RAG vs Fine-Tuning — Resolved

Graph-as-retrieval-store via MCP tools. No fine-tuning required for initial delivery. The agent queries the graph; the graph answers deterministically. Fine-tuning is a separate decision, explicitly deferred to Plans 10–11.

---

## MCP Server

Custom-built MCP serving surface over the graph stores (Neo4j for repo graphs; store TBD for others). Seven tools initially, expanding as graphs come online. All responses include `generated_at` timestamp from the index — not the cache. Agent surfaces data age in every reply. If data is more than 25 hours old, agent flags it explicitly.

Note: the MCP server is our build, not delegated to a graph tool's built-in serving. This is required because the serving layer must enforce Tier 1 constraints, attach confidence/coverage metadata, and emit the node IDs that feed decision provenance — none of which a generic graph-serving tool provides.

| Tool | Source graph |
|---|---|
| `get_code_semantics` | Repo graphs |
| `get_change_history` | Repo graphs |
| `get_safety_context` | DCB0129 graph |
| `get_integration_surface` | NHS integration graph |
| `get_support_context` | ServiceNow graph |
| `get_capability` | Capability catalogue |
| `get_ui_pattern` | UI Kit graph |

---

## Two Interfaces on Top of the Graph Family

### Genesis AI Pipeline — Retrieval-Augmented Design Engine
The pipeline stops being an interview engine and becomes a retrieval-augmented design engine. When a new requirement comes in, the agent queries the graph family first:

- Capability catalogue — does this already exist? What does it do today?
- Repo graphs — where is it implemented, what does it couple to, what is the migration status?
- DCB0129 graph — are there existing hazards?
- Security and IG graphs — what controls already apply?
- UI Kit graph — what components and patterns are already established?
- Confluence via capability catalogue — what was the original design intent?

Questions become exceptions — gaps where the graph has no answer. The user confirms, challenges, or extends what the graph already knows. Requirements by exception. Architecture by exception. Safety by exception. Design by exception.

### Internal Chat Interface
Natural language Q&A across the full graph family. Any engineer, support analyst, or product person can ask questions grounded in 25 years of EMIS knowledge. The agent doesn't just retrieve — it traverses. If the answer isn't in the top-level query, the agent hunts across co-change edges, adjacent namespaces, ticket history, and manuals until it finds a grounded answer or explicitly states what it couldn't find and why.

Quality improves as graphs mature. Early on the agent sometimes comes back empty. Over time as resolution patterns accumulate and Confluence is ingested, the hunts get shorter and the answers get better — without any model retraining.

---

## Use Cases — How This Transforms the Organisation

### Development
An engineer picks up a ServiceNow ticket. Before they open the codebase the agent has already queried the repo graph, surfaced co-change coupling history, identified the files that have historically moved together, flagged the DCB0129 hazard classification, and pulled previous tickets with identical error patterns and their resolutions. Two hours of archaeology becomes five minutes. The fix is scoped correctly first time because the blast radius was known before a line was touched.

### Support — Closed Loop with Automatic PR
A bug is reported. The agent queries the ServiceNow graph, repo graph, and DCB0129 graph. It identifies the likely cause, scopes the fix, checks the blast radius, verifies clinical namespace handling. It writes the fix, generates tests first (TDD rule), raises a PR against the correct repo. The six CI gates run automatically. The PR lands in the engineering team's review queue with full context — what the ticket was, what the agent diagnosed, what it changed and why, which gate results passed.

The engineer reviews a fully formed, tested, context-rich PR rather than a raw ticket. Their job becomes approval and clinical judgement, not archaeology and implementation. Tickets that currently take days become hours. Every resolved ticket feeds back into the graph — fix pattern, co-change coupling confirmed, resolution notes. The next similar ticket is diagnosed faster.

The CI gates enforce correctness structurally. A support fix touching a clinical namespace without a hazard ID in the PR description cannot merge. The guardrails are in the pipeline, not in the prompt.

### Bids — Bid Intelligence Engine
A bid for a new NHS contract requires a response to forty functional requirements. For each requirement the agent returns one of three answers:

**Already delivered** — capability exists in the catalogue, here is the evidence, confidence high. Bid team commit with zero risk.

**Gap — assessable** — capability doesn't exist but the repo graph knows the blast radius. Returns estimated effort, risk level, what would need to change, whether existing patterns make it straightforward or whether it touches volatile high-risk namespaces. Bid team price it with real data behind the number.

**Gap — alternative** — capability doesn't exist and the effort or risk is too high for this bid cycle. Agent queries the capability catalogue for what does exist that partially satisfies the requirement, surfaces the delta, and proposes a compliant alternative with a draft narrative. Turns a hard no into a negotiated position. Particularly powerful in NHS procurement where requirements are often written loosely enough that a well-argued alternative satisfies the intent.

The bid team stops being order takers and starts operating with full situational awareness of what EMIS can genuinely deliver, at what cost, and with what alternatives when it can't.

### Planning and Prioritisation
Engineering leadership wants to know which EMIS Web modules to migrate first. The repo graph surfaces complexity scores, commit volatility, co-change coupling breadth, and DCB0129 hazard classifications for every module. The ServiceNow graph shows which modules generate the highest support ticket volume. The strangler boundary auto-deriver shows what is already in flight. Migration sequencing becomes a data-driven decision rather than a negotiation between teams with competing priorities.

### Onboarding
A new engineer joins the EMIS-X programme. On day one they have access to the full graph. They ask questions in natural language via the internal chat interface. Their first PR looks like it was written by someone with two years of EMIS context because the agent that helped them carried that context. Time to first meaningful contribution drops from months to weeks.

### Training
A new clinical safety reviewer needs to understand DCB0129 obligations across the prescribing domain. The graph surfaces every hazard ever filed against prescribing namespaces, the mitigations applied, the audit trail of code changes linked to each hazard, and the clinical safety cases that closed them. Training that previously required months of shadowing an experienced CSO becomes a structured query session against 25 years of real decisions.

### Innovation
A product team wants to explore a new AI-assisted consultation summarisation capability. The agent queries the capability catalogue for existing consultation functionality, the repo graph for the implementation surface, the DCB0129 graph for clinical risk classification, and the IG graph for data processing obligations. Before a single line of prototype code is written the team knows what exists, what the risk profile is, what the IG constraints are, and where the genuine innovation sits versus the commodity reuse. Feasibility work that previously took months takes days.

---

## Ground Truth — The Central Principle

This is the most important principle in Workstream C and must be understood before any build decision.

The context graph is not a suggestion engine and not a RAG system that returns matches for the LLM to reason around freely. It is the ground truth layer. But "ground truth" needs a precise definition, because getting it wrong in either direction is costly.

**The graph is a projection, not the source.** For anything with a more primary source, the graph is a fast queryable projection of that source — not the source itself. The code is ground truth; the repo graph is a projection of the code. The DCB0129 Excel is ground truth; the clinical safety graph is a projection of it. This framing dictates what you do when graph and source disagree: you re-derive the graph from the source. You never patch the graph to override its source. The moment someone "fixes" the graph directly, it drifts from the thing it represents and the guarantee is gone.

**Read-only to agents — non-negotiable architectural rule.** No agent writes to any graph directly. Every enrichment comes through a certified path — a merged PR, an approved artefact, a human sign-off. This rule is what makes the ground truth guarantee hold. It must not be eroded under delivery pressure.

### Tiered Trust Model

Pure constraint is correct for regulated graphs and too brittle for the rest. The graph will never have perfect coverage — there will always be unindexed namespaces, uncatalogued capabilities, unmapped tickets. If the LLM is hard-constrained by what the graph knows, it is also crippled by what the graph does not yet know. That is the wrong failure mode for a productivity tool. The model is therefore tiered by graph, not uniform.

**Tier 1 — Constrain (regulated graphs: DCB0129, Security, IG).** High confidence, human certified. Hard constraints. If the graph says stop, the agent stops — no negotiation, no reasoning around it. A missed clinical hazard classification is never an acceptable failure mode. The graph is both floor and ceiling here.

**Tier 2 — Ground and extend (repo graphs, capability catalogue, ServiceNow, manuals).** The graph is the floor, never the ceiling. The agent grounds its reasoning in what the graph has certified and never contradicts it — but where the graph has no signal, the agent may extend beyond it, explicitly flagging that it is doing so: "The graph tells me this. Beyond that I have no grounded data and am reasoning from general knowledge — treat with lower confidence and validate." A new engineer working on a partially indexed namespace still gets useful output. Under pure constraint that same namespace would produce an agent that refuses to help until the graph catches up — which kills adoption in the early months when coverage is still building.

**Tier 3 — Signal only (null or very low confidence nodes).** Surfaced as context with an explicit warning: insufficient signal to treat as ground truth, use as a starting point only.

### Graph accuracy is the primary engineering discipline
The risk is not that the LLM gives a vague answer because a graph is thin. The risk is that a graph contains inaccurate data and the LLM acts on it confidently. A wrong co-change coupling produces a confidently wrong blast radius. A missing DCB0129 classification silently passes something that should have been caught. Accuracy — not coverage, not latency — is the discipline that matters most.

---

## Cross-Cutting Design Principles

These principles apply across all graphs and must be designed in from the start, not retrofitted.

**1. Conflict detection is a feature, not just a safeguard.** Graphs will disagree. The capability catalogue says a capability does X; the repo graph, derived from code, shows it does Y. Precedence rule: the repo graph wins on "what the code does", the capability catalogue wins on "what it is meant to do", and the delta between them is a first-class output — it surfaces documentation drift that nothing currently tracks. Design for conflict detection rather than letting one graph silently override another.

**2. Measure coverage explicitly — the graph must know what it does not know.** Coverage gaps are invisible unless measured. A namespace with zero DCB0129 nodes is either genuinely non-clinical or not yet tagged — opposite meanings the system must distinguish. Every graph carries a coverage metric per namespace. Absence of a flag must never be silently interpreted as safe.

**3. Provenance snapshots content, not just node IDs.** The graph changes nightly. An artefact generated today was grounded in today's graph. To investigate a decision in three weeks — essential in a regulated medical context — the provenance must capture what each node said at the time, not just its ID. Point-in-time reconstruction is not optional. Cheap to build now, very expensive to retrofit.

**4. Confidence decays with age.** A node last validated two years ago is not as trustworthy as one validated last month, even at the same score. Confidence discounts with age unless refreshed by recent signal. Stale-but-once-trusted nodes are the most dangerous kind — high confidence, no recent validation.

**5. Separate drift detection from confidence scoring.** Code that merged, compiled, passed tests, and shipped is trustworthy because it is real — no feedback loop is needed to establish that. What the feedback mechanism is actually for is narrower: detecting when the graph has drifted from reality (a coupling that no longer holds, a deprecated capability). Build drift detection first. It is cheaper and targets the real risk. Add per-node confidence scoring only if drift detection proves insufficient — do not build the elaborate version first.

**6. Just-in-time human certification for Tier 1 graphs.** 25 years of hazard history cannot be certified up front. Certify namespaces in migration order, just ahead of each capability entering the pipeline. Big-bang certification blocks the graph on human throughput before it delivers value.

---

## Evaluation Harness — Prove Value Before Scale

**This is the single most important prerequisite and it comes before Phase 1, not after.**

Everything in this design assumes the graph makes the agent better. Without a way to prove it, you cannot tune the graph, cannot know if it is helping, and cannot defend the investment to TPG.

Before building eight graphs, build a benchmark: a set of real EMIS tasks with known-good outcomes, run with and without graph context, measured for quality difference. The Week 1 spike's deliverable is not "the indexer runs" — it is "the indexer measurably improves output on ten real tasks." Reframe the spike around proof, not function. If the graph does not demonstrably improve outcomes on the benchmark, that is a finding worth having before spending the rest of the budget.

The benchmark becomes the permanent regression harness — every graph change, every indexer improvement, every new source is validated against it. Quality is measured, not assumed.

---

## Provenance — Carrying State Through the Pipeline

The feedback loop that corrects the graph only works if every agent output carries a provenance trail — which graph nodes were queried and which nodes contributed context to each specific output. Without provenance you cannot trace a failure or a success back to the node that caused it. The correction has nowhere to write back to.

**How provenance is captured**

Every MCP response includes the node IDs that contributed the context. The Genesis AI pipeline captures those node IDs and stores them against the artefact at the point of generation. The artefact record in the DB carries a `graph_provenance` field:

```json
{
  "artefact_id": "REQ-042",
  "graph_provenance": [
    { "graph": "repo", "node_id": "abc123", "namespace": "EMIS.Prescribing.Dispensing", "confidence": 0.87 },
    { "graph": "capability_catalogue", "node_id": "def456", "capability": "dispense_medication", "confidence": 0.91 },
    { "graph": "dcb0129", "node_id": "ghi789", "hazard_id": "HAZ-014", "confidence": 1.0 }
  ]
}
```

Provenance accumulates and extends through every pipeline stage. P01 captures the initial nodes. P03 adds architecture-specific nodes. P04 adds schema nodes. By the time P11 generates code, the provenance trail covers every graph node that influenced every decision from requirements through to implementation.

**How provenance travels to Git**

When `genesis-ai-bot` commits code, the provenance is written into the Git commit trailer:

```
Genesis-Graph-Nodes: abc123,def456,ghi789
Genesis-Artefact: REQ-042
Genesis-AI-Version: 1.4.2
```

This means the connection between a merged commit and the graph nodes that produced it is permanently recorded in Git history. It cannot be lost. It does not require a separate audit system.

**Implementation note**

This is not a new system. It is two additions to what already exists — a `graph_provenance` collection on the `Conversation` entity, and graph node IDs in the `genesis-ai-bot` commit trailer. The infrastructure, event system, and commit pattern are unchanged.

---

## The Feedback Loop — Drift Detection First, Confidence Scoring Later

Manual curation does not scale across eight graphs and 25 years of history. The graph maintains its own accuracy through production outcomes. But build this in the right order — the simple version first.

### Stage 1 — Drift detection (build this first)

Code that merged, compiled, passed tests, and shipped is trustworthy because it is real. You do not need a scoring system to establish that. What you need is detection of when the graph has drifted from reality.

On merge, a GitHub Actions workflow reads the `Genesis-Graph-Nodes` trailer and compares what the graph asserted against what the merged code actually shows. Where they disagree — the graph claimed a coupling that the merged code does not exhibit, or claimed a capability that no longer exists — a drift flag is raised against that node. The node is surfaced to the domain curator for investigation. This is cheap to build, targets the real risk, and requires no statistical machinery.

### Stage 2 — Confidence scoring (add only if Stage 1 proves insufficient)

Per-node confidence scoring — divergence scores accumulating into a weighted confidence per node — is the sophisticated version. It has three known weaknesses: attribution is hard (one merge signal spread across many contributing nodes), sparsity (most nodes see too few PRs to reach a meaningful sample size), and false signals (an engineer rewriting correct output for a scope change wrongly penalises a good node).

Do not build this first. Build drift detection, run it, and only add confidence scoring if drift detection proves insufficient to keep the graph accurate. If confidence scoring is built, it must include: a reason code on merge to separate divergence-because-wrong from divergence-because-changed, a minimum sample size before a score is meaningful, and age-based decay so stale nodes are discounted.

### Certification tiers

Tier 1 graphs (DCB0129, Security, IG) — human certified before entries are ground truth. Drift detection flags for human review but never auto-promotes or auto-demotes. A confidently wrong clinical or security classification is not an acceptable failure mode.

Tier 2 graphs (repo, capability catalogue, ServiceNow, manuals) — drift detection runs automatically on merge. Curator investigates flagged nodes. Source is corrected; graph is re-derived.

---

## Bad Provenance — Detection, Trace, and Correction

This is where the design gets hard. A bad graph node contributes bad context. The LLM acts on it confidently. The output is wrong. When is it caught, how is it traced, and what happens next?

**When it is detected**

Bad provenance does not surface immediately. It travels silently through pipeline stages that build on the bad context rather than challenging it. Three detection points exist:

**CI gate failure on merge** — the code implementing the bad architecture fails a structural gate. The strangler boundary gate flags the implementation touches a namespace already live in EMIS-X that should not be modified. Or the DCB0129 gate flags a code path that bypasses a required hazard mitigation. The PR cannot merge. The engineer investigates and traces back to the architectural decision and the graph node that produced it.

**High divergence on merge** — the CI gates pass but the engineer rewrites the agent's proposed implementation substantially before merging. The feedback loop fires with a high divergence score. Confidence on the contributing nodes drops. If the same nodes accumulate high divergence scores across multiple PRs, they are automatically flagged for curator investigation.

**P06 or P08 reviewer rejection** — the clinical safety or security reviewer rejects an artefact because the architecture it is based on does not adequately address a hazard or a control. The session is paused. The provenance trail is the starting point for investigation.

**The trace**

Every detection point carries enough information to trace back to the source. The PR carries the `Genesis-Graph-Nodes` trailer. The artefact record carries the full `graph_provenance` chain from P01 onwards. The graph service can identify which specific node contributed the bad context and at which pipeline stage it entered the session.

The feedback write-back fires immediately on detection:

```json
{
  "node_ids": ["arch-node-xyz"],
  "pr_id": "4521",
  "divergence_score": 0.91,
  "outcome": "rejected"
}
```

Confidence drops. Below 0.5 the node is automatically flagged — not just downweighted, actively surfaced to the graph curator as requiring investigation.

**The three root causes and their corrections**

**Stale source data** — the node describes a namespace coupling that was true in 2023 but a subsequent refactor broke it. The nightly indexer has not picked up the change because the refactor was in a branch that was not indexed. Fix: trigger a manual re-index of the affected namespace. Next nightly run pulls the corrected data. Node confidence recovers as new PRs using the corrected node pass cleanly.

**Indexer logic error** — the Roslyn indexer is misclassifying a coupling because it reads a dependency injection registration as a hard dependency when it is actually a runtime swap. Fix: engineering change to the indexer logic, re-index, re-verify. More expensive but targeted — one indexer fix corrects the classification across every node affected by the same pattern.

**Architecture genuinely changed** — EMIS-X introduced a new service boundary that the graph does not yet know about. The strangler manifest needs updating. Fix: update the boundary definition, re-derive from API gateway traffic logs, re-index. The node is corrected to reflect the current EMIS-X state.

In every case the fix is targeted. One node, one root cause, one correction. The fix propagates immediately to every future query that touches that node.

**What happens to an in-flight session**

If a bad provenance node contributed to a session that is mid-flight — requirements approved, architecture approved, currently in P06 — the session does not restart automatically. That would be unworkable.

Instead the graph service raises a provenance alert against the active session: "A graph node that contributed context to the P03 architecture artefact in this session has been flagged as low confidence. Review recommended before proceeding to P11."

The P10 pre-swarm gate is the natural catch point. Before the swarm starts, P10 checks active provenance alerts on the session. If a flagged node exists, the P10 gate fails until the specific architectural decision based on that node has been reviewed and either reconfirmed or corrected by a human. This is precisely why P10 exists — not just as a cost gate, but as the last human checkpoint before expensive code generation, designed to catch exactly this scenario.

---

## Worked Example — Appointment Booking P01 to P11

> **Note:** This is an illustrative example to make the graph behaviour concrete. The specific confidence scores, node IDs, and ticket counts are representative, not real. The architectural patterns, clinical hazards, and pipeline flow reflect the actual Genesis AI design.

---

**P01 — Requirements Discovery**

A BA opens a requirements session for appointment booking in EMIS-X. Before the first question is asked the agent queries the graph family simultaneously:

- Capability catalogue — appointment booking exists in EMIS Web. Slot types, session types, GP link, DNA tracking, online booking surface. Confidence: 0.91.
- Repo graph — `EMIS.Appointments.Booking` namespace. Complexity: medium. Co-change coupling: tightly coupled to `EMIS.Patients.Demographics` and `EMIS.Clinical.Sessions`. 67 merged PRs providing signal.
- DCB0129 graph — two active hazards: HAZ-031 (double booking risk on concurrent slot requests), HAZ-044 (patient identity mismatch risk on online booking). Both surface before a single requirement is written.
- ServiceNow graph — top three recurring support tickets: slot availability not refreshing, DNA status not updating, online booking failing for patients with no NHS number.

The agent opens with a structured brief, not twenty questions:

*"Here is what EMIS Web appointment booking currently does. Here are the two active clinical hazards you must account for. Here are the three most common failure patterns reported by practices. What does EMIS-X need to do differently, better, or additionally?"*

The BA confirms, challenges, and extends — not builds from scratch. REQ artefact generated. Graph provenance captured: capability catalogue node `def456`, repo graph node `abc123`, DCB0129 nodes `ghi789` (HAZ-031) and `jkl012` (HAZ-044).

---

**P02 — Prototype Demo Builder**

Agent queries the UI Kit graph. Appointment booking UI patterns exist — slot grid component, availability indicator, patient search typeahead, confirmation modal. Prototype generated using established components and correct CSS tokens. No hallucinated components. No design system violations. UI Kit node IDs added to provenance.

---

**P03 — Architecture**

Agent queries the repo graph for the existing EMIS Web appointment booking architecture — data flow, service boundaries, external integrations (NHS Spine PDS lookup, online booking API gateway). Strangler boundary auto-derived: three components already live in EMIS-X, two in flight, four still legacy.

Architecture is by exception. The agent proposes the delta — what needs to change for EMIS-X and why. HAZ-031 and HAZ-044 are already in session context from P01. Concurrent slot request handling and patient identity verification are not optional — they are architectural constraints before the ARCH artefact is written. Repo graph and strangler boundary nodes added to provenance.

---

**P04 — Design (API and DB)**

Agent queries the repo graph for the existing EMIS Web appointments DB schema — tables, columns, constraints, indexes, foreign keys. Proposes the delta against the existing schema, not a schema from scratch. Co-change coupling flag: `appointments.slots` and `patients.demographics` have co-changed in 34 of the last 50 migrations. Any schema change to slots is flagged for simultaneous demographics review. Schema nodes added to provenance.

---

**P05 — PxD**

Agent queries UI Kit graph and capability catalogue together. Interaction design for slot selection, confirmation flow, and DNA handling already established. PxD covers genuine design decisions — the new EMIS-X experience layer — not boilerplate.

---

**P06 — Clinical Safety**

HAZ-031 and HAZ-044 have been in session context since P01. The DCB0129 artefact is not written cold — it is written against the existing hazard record, with EMIS Web mitigations surfaced as the baseline. The CSO reviews: are existing mitigations sufficient, or does the new EMIS-X architecture introduce new risk? Full clinical safety history of this capability available in context. CI gate checks: every HAZ-ID referenced in the artefact must exist in the DCB0129 graph with a linked mitigation. Structurally enforced, not prompt-based.

---

**P07 — Information Governance**

Agent queries the IG graph. Appointment booking touches patient demographics, clinical session data, NHS number, online consent. IG obligations for each data type already in the graph — Article 9 special category handling, DSPT obligations, NHS Spine data sharing agreements. DPIA is by exception against what the IG graph already knows. IG owner reviews the delta.

---

**P08 — Security**

Agent queries the security graph and repo graph together. Security graph knows controls that apply to patient-facing APIs — authentication requirements, rate limiting, audit logging standards. Repo graph knows the existing EMIS Web security implementation. Security reviewer sees: existing security posture, what the EMIS-X architecture changes, where the delta introduces new attack surface. SEC artefact covers genuine security decisions, not a generic checklist.

---

**P10 — Pre-Swarm Decision Gate**

Agent surfaces the full provenance trail accumulated from P01 to P08. Every graph node listed with its current confidence score. Before the swarm starts, the human review gate sees which parts of the design are grounded in high-confidence graph data and which are grounded in low-confidence or unverified nodes.

Any active provenance alerts — nodes that have been flagged as low confidence since the session started — surface here. The P10 gate fails if a flagged node contributed to an architectural or clinical decision that has not been reviewed and confirmed by a human. The swarm does not start until this gate passes.

---

**P11 — TDD and Code Generation**

The TDD agent writes tests grounded in the REQ artefact — which was grounded in the capability catalogue and DCB0129 graph. Tests are derived from real EMIS Web behaviour and real clinical hazards, not invented. The code agent writes implementation grounded in the repo graph — existing namespace structure, existing patterns, existing DB schema delta. It implements the delta against what already exists.

The PR is raised by `genesis-ai-bot`. The `Genesis-Graph-Nodes` trailer carries the full provenance chain from P01 through P11. Six CI gates run: DCB0129 hazard ID check, strangler boundary alignment, NHS integration contract, TDD compliance, co-change coupling alert, security controls. The PR cannot merge without passing all six.

On merge, the feedback workflow fires. Divergence computed per namespace. Confidence scores updated on all contributing nodes. Appointment booking namespace nodes accumulate another data point. The next capability touching this namespace starts from higher-confidence ground truth than this one did.

---

**What this achieves**

A requirements-to-production cycle that currently takes weeks of workshops, manual archaeology, and repeated context reconstruction ran as a continuous grounded session. Every stage started from what the graph already knew. Every decision is traceable to a source node with a confidence score. Every output is an input to the next stage and a feedback signal back to the graph. Velocity compounds every sprint because the graph compounds every sprint.

---

## Execution Roadmap — Incremental to End State

The roadmap is deliberately incremental. Each stage delivers standalone value, is validated before the next begins, and builds toward the end state without requiring the end state to be designed in full up front. The governing rule: prove value at each stage before funding the next. Do not build sophistication ahead of demonstrated need.

### Stage 0 — Proof (before committing to the full build)
**Goal: prove the graph improves agent output. Kill criterion if it does not.**

- Build the evaluation benchmark — ten real EMIS tasks with known-good outcomes.
- Run the Roslyn + Neo4j stack against one well-understood repo (already in progress — team giving decent initial results against EMIS Web).
- Measure agent output quality with and without graph context against the benchmark.
- **Gate:** measurable quality improvement on the benchmark. If quality does not improve, stop and reassess before spending further budget.

Deliverable: evidence, not infrastructure. This is the go/no-go for the whole workstream.

*Note: the Graphify spike is already complete — tested poorly against EMIS Web, rejected for .NET repos per ADR-C2. Roslyn + Neo4j is the validated stack.*

### Stage 1 — Thin vertical slice (one repo, end to end)
**Goal: one real task completed through the graph, start to finish.**

- Roslyn extraction for one repo loaded into Neo4j, refreshed on a defined cadence (nightly or diff-based — resolve in the fit-together session).
- MCP serving surface built over Neo4j exposing `get_code_semantics` and `get_change_history`.
- Provenance capture (node IDs + point-in-time content snapshot) wired into the pipeline from day one — our build on top of the Neo4j graph.
- One developer connected. One real ticket resolved with a merged PR — not a demo.
- Drift detection Stage 1 live on that repo — comparing the graph's asserted relationships against merged code.

**Gate:** the real PR merges clean and the drift detector correctly flags at least one seeded stale node.

### Stage 2 — Repo graph scale + coverage measurement
**Goal: all UK engineers on repo graphs, with coverage visible.**

- Full codebase indexed across repos. Graph-per-repo boundary enforced.
- Coverage metric per namespace live — the graph knows what it does not know.
- Strangler boundary auto-derived from API gateway traffic logs.
- Conflict detection between repo graph and any existing documentation surfaced as a first-class output.
- All UK engineers connected via the internal chat interface (Tier 2 ground-and-extend model).

**Gate:** coverage metric trusted, strangler boundary validated against known migration state.

### Stage 3 — Capability catalogue + ServiceNow
**Goal: business-level querying and the support-to-PR loop.**

- Capability catalogue derived from repo graphs.
- ServiceNow graph live at 70%+ namespace mapping coverage.
- Support-to-PR closed loop piloted on one recurring ticket type.
- `get_capability` and `get_support_context` added to MCP.

**Gate:** support-to-PR loop produces a mergeable PR on a real ticket; capability catalogue conflict detection surfaces at least one real documentation-drift instance.

### Stage 4 — Enrichment (Confluence + manuals + UI Kit)
**Goal: the "why" and the design system in context.**

- Confluence enrichment of the capability catalogue (Graphify).
- Manuals and guides graph (Graphify).
- UI Kit graph for prototype and design stages.
- DB schema ingested; `get_ui_pattern` and architecture context added to MCP.

**Gate:** requirements-by-exception demonstrably reduces question count on a real session versus baseline.

### Stage 5 — Regulated graphs (DCB0129, Security, IG)
**Goal: Tier 1 constraint graphs live, just-in-time certified.**

- DCB0129 tool built — Excel-per-area ingestion, CSO owned, one schema per area.
- Security and IG graphs built once sources confirmed with respective teams.
- Just-in-time human certification — namespaces certified in migration order, ahead of each capability entering the pipeline.
- Tier 1 hard constraints enforced via system prompt and CI gates.
- Interim static namespace blocklist active throughout until DCB0129 graph is certified for a given area.

**Gate:** CSO sign-off process operating; first clinical migration sprint runs under Tier 1 constraint with hazard IDs enforced by CI gate.

### Stage 6 — Full pipeline integration (P01–P11 grounded)
**Goal: the worked example becomes the default way of working.**

- All graphs queried across the full pipeline. Provenance chain complete P01 → P11.
- P10 pre-swarm gate checks provenance alerts before code generation.
- Bad-provenance detection, trace, and correction operating end to end.

**Gate:** a full capability migrated end to end, entirely graph-grounded, benchmark quality maintained.

### Stage 7 — Sophistication (add only where proven necessary)
**Goal: add the elaborate mechanisms only where the simple versions fell short.**

- Per-node confidence scoring — only if drift detection proved insufficient at Stage 1–2.
- Confidence decay and reason-coded divergence — only if confidence scoring is built.
- Graph merge decisions — only where query patterns proved a real cross-graph need.
- Fine-tuning (deferred to Plans 10–11) — only after RAG-via-MCP economics are proven.

This stage is explicitly conditional. Each item is built only against demonstrated need from earlier stages, never speculatively.

---

**Dependency note:** Stages 0–2 are gated on Plans 3c/3d and TPG partner introductions per the original blockers. Stages 3 onward assume the partner engagement is active. The evaluation benchmark (Stage 0) can be designed internally now — it does not depend on TPG.

---

## Graph Construction Tooling — Roslyn + Neo4j for .NET, Graphify for Docs

**Decision (revised after team testing against EMIS Web, June 2026).** Roslyn + Neo4j is the repo graph construction stack for the .NET estate. Graphify is retained only for non-.NET, document and multi-modal graphs where its tree-sitter and PDF/Confluence extraction are a genuine fit.

### Why the position changed
An earlier draft (ADR-C1) proposed adopting Graphify as the Tier 2 construction layer for repo graphs, conditional on a spike. The team ran that spike. **Graphify performed poorly against EMIS Web.** This is the Stage 0 gate working as designed — the spike caught the problem before build.

The root cause is architectural. Graphify's C# support is tree-sitter, which gives syntactic structure but not semantic resolution. Roslyn compiles the solution and resolves symbols, overloads, generics, interface implementations, and cross-project references. For a codebase of EMIS Web's age and size, that semantic depth is the difference between a usable graph and one that is not. Tree-sitter was never going to be the C# indexer; the spike proved it empirically.

### Repo graph stack — Roslyn + Neo4j
- **Roslyn** — compiles EMIS Web, resolves full semantic model, extracts nodes and edges with real symbol resolution.
- **Neo4j** — graph store. Cypher query surface. EMIS-specific enrichment layered on top of the extracted graph.
- **Status:** giving decent initial results in team testing. Known teething issues being worked through — notably Neo4j default query/row limits that must be overridden for EMIS Web scale, plus the usual index strategy, pagination, and variable-length-path-bounding work that any Neo4j-at-scale deployment requires.

### Where Graphify still fits
- **Manuals and guides graph** — PDF/Markdown/diagram multi-modal extraction. Genuine fit.
- **Confluence enrichment of the capability catalogue** — document semantic extraction. Genuine fit.
- **Non-.NET repos where semantic resolution is less critical** — candidate, not committed. Evaluate per repo.

Graphify is not used for any .NET repo graph. That is settled.

### What stays our build regardless of tooling (the moat)
Independent of whether Roslyn+Neo4j or Graphify builds a given graph, the following is EMIS regulatory and pipeline logic and is the differentiation:

- Clinical namespace tagging against DCB0129 hazards
- Strangler fig boundary classification derived from API gateway traffic logs
- Tier 1 human-certification workflow (DCB0129, Security, IG)
- The `Genesis-Graph-Nodes` provenance trailer written into `genesis-ai-bot` commits — decision provenance, snapshotted, traceable from a merged PR back through P01–P11
- Pipeline-by-exception integration across P01–P11
- CI gates that block a merge when a clinical namespace lacks a hazard ID

### Open questions for the fit-together session (Idris + Luke + team)
The team has done enough research to warrant an architecture fit-together conversation. Questions to resolve in that session:

1. **Neo4j deployment model** — self-managed on EC2 or AuraDB? Does it sit inside the VPC on the sovereign boundary? EMIS Web code IP requires this even though it is not patient data.
2. **Roslyn node/edge schema** — is there a defined schema for what Roslyn extracts, or is it emergent? This determines how the EMIS enrichment layers (clinical namespace tagging, strangler classification) attach.
3. **Legacy / non-Roslyn code** — how is VB6 and any other non-Roslyn-parseable EMIS Web code handled? Roslyn covers the C#; name the coverage gap now.
4. **Incremental update model** — full nightly re-extract, or diff-based? At EMIS Web size a full re-parse may be too slow for a nightly cadence.
5. **Neo4j query limits** — the override approach for EMIS Web scale, plus index strategy on the most-traversed node labels and bounding on variable-length path queries.
6. **How it fits the wider Genesis AI architecture** — MCP serving surface over Neo4j, provenance capture integration, and the enrichment layer attachment points.

---

## Open Questions — AMP v3.0

| Q | Question | Status |
|---|---|---|
| Q1 | Which complexity dimension is most underdocumented? | ❓ Open — internal |
| Q2 | API gateway log group exists and is queryable for strangler boundary auto-deriver? | ❓ Open — infra team |
| Q3 | GitHub org accessible from VPC? PAT or GHES constraints? | ❓ Open — infra team |
| Q4 | DCB0129 hazard log format? | ✅ Excel, one file per clinical safety area, CSO owned, nightly import to DB |
| Q5 | Nightly CI for indexers? | ✅ Yes |
| Q6 | ServiceNow API access? Authentication? DBA approval needed? | ❓ Open — internal |
| Q7 | Okta as identity provider for VPC ALB? | ❓ Open — infra team |
| Q8 | GitHub Actions or ADO — which CI/CD? Separate REST endpoint needed for CI gates? | ✅ Separate lightweight REST endpoint for CI gates — MCP server is for IDE/agent queries only |
| Q9 | Who can override a Step 0 clinical block — CSO alone or CSO + EM? | ❓ Open — policy decision |
| Q10 | Timeline realistic? | ✅ v6.0 extends to 16 weeks — this is the working plan |

Internal questions (Q1, Q6, Q9) resolvable without TPG. Infra questions (Q2, Q3, Q7) need the infra team. TPG conversation is about methodology and tooling — not blocked by any of the above.

---

## Metrics

### Engineering (weekly)
- Time from ticket open to proposed fix — target < 30 minutes by Week 4
- HIGH divergence namespaces — target < 20% of total by Week 10
- Graph freshness violations — zero responses older than 25 hours

### Support operations (weekly)
- ServiceNow mapping coverage — target 85%+ by Week 10
- Stale map entries — zero (alert if non-empty)
- Ticket-to-PR time — reduction vs baseline by Week 6

### Leadership (monthly)
- Support ticket volume by namespace — informs migration prioritisation
- Average resolution time by product area
- Migration sprint velocity — modules per sprint
- Clinical PRs requiring CSO review vs total clinical PRs — DCB0129 compliance signal for TPG and NHS England
- Bid win rate on requirements assessed as delivered or alternative proposed

---

## Architecture Decisions

**ADR-C1 — Graphify adopted as Tier 2 graph construction and serving layer.**
*Status: SUPERSEDED by ADR-C2 (team testing against EMIS Web, June 2026). Retained for the record.*

Original decision was to adopt Graphify as the construction and MCP serving layer for Tier 2 graphs including repo graphs, conditional on a Stage 0 spike. The spike ran. Graphify performed poorly against EMIS Web because its C# support is tree-sitter (syntactic) rather than semantic. The conditional was not met. Superseded.

The general principle from ADR-C1 still holds: EMIS-specific enrichment, decision provenance, certification, and pipeline integration are always our build, independent of construction tooling. Only the choice of construction tool for .NET repos changed.

---

**ADR-C2 — Roslyn + Neo4j for .NET repo graphs; Graphify retained for docs only.**
*Status: Accepted (team-validated against EMIS Web, June 2026). Supersedes ADR-C1.*

Decision: Roslyn + Neo4j is the repo graph construction stack for the .NET estate. Graphify is retained only for non-.NET document and multi-modal graphs (manuals, guides, Confluence enrichment).

Rationale: Roslyn compiles the solution and resolves the full semantic model — symbols, overloads, generics, interface implementations, cross-project references. Tree-sitter tools give syntactic structure only, which tested poorly against EMIS Web's size and age. Neo4j is the graph store with a Cypher query surface. The team validated this stack empirically and it is giving decent initial results.

Consequences: known Neo4j-at-scale work required — default query/row limit overrides for EMIS Web scale, index strategy, pagination, variable-length path bounding. Non-Roslyn legacy code (VB6, etc.) is a coverage gap to be scoped. Deployment model, extraction schema, and incremental update cadence to be resolved in the Idris + Luke fit-together session.

---



---

## Next Steps

Workstream C does not start until:
1. Plan 3c complete
2. Plan 3d complete
3. TPG introductions confirmed for specialist partner

**Near-term:** book the architecture fit-together session (Idris + Luke + team) — the team has done enough Roslyn/Neo4j research to make it worthwhile. Agenda is the six open questions in the Graph Construction Tooling section.

Internal pre-work that can happen in parallel:
- Design the evaluation benchmark — ten real EMIS tasks with known-good outcomes (Stage 0 deliverable, no external dependency)
- Resolve the six Roslyn/Neo4j fit-together questions (deployment model, extraction schema, legacy code coverage, incremental update, query limits, wider architecture fit)
- Answer Q1, Q6, Q9 (no external dependency)
- Confirm API gateway log group queryable (Q2)
- Confirm GitHub org VPC access and CI/CD platform (Q3, Q7)
- CSO to confirm complete list of clinically classified namespace prefixes for day-one blocklist
- ServiceNow API access and authentication mechanism (Q6)

---

*Document owner: Idris Issa | Version: 7.0 | Classification: Internal — Confidential*
