# Workstream C — Context Graph: Design Decisions

**Status:** Not Started — blocked on Plans 3c and 3d completing, then TPG partner introductions.  
**Session date:** June 2026  
**Owner:** Idris Issa  
**Classification:** Internal — Confidential

---

## What This Document Is

Design decisions and planning output from the first Workstream C planning session. Captures resolved decisions, open questions, and the agreed delivery approach. To be updated as further decisions are made.

---

## Reference Documents

Two documents inform the design and should be read alongside this:

- **EMIS_AMP_v3.0** — EMIS Web AI Migration Platform. Five indexers, five MCP tools, six-gate CI/CD pipeline, 10-week adoption roadmap. Contains ten open questions to be answered before v1.0 baseline.
- **EMIS_ContextGraph_v6.0** — Realistic delivery plan. Addresses six concerns from v5.0 review. Thin-layer spike-first approach. 16-week phased delivery. The working delivery plan for Workstream C.

---

## Confirmed Data Sources

Five sources. Three structured and ready. Two requiring formalisation.

| Source | Status | Phase |
|---|---|---|
| Roslyn AST | ✅ Structured, ready to index | Phase 1 |
| GitHub history | ✅ Structured, ready to index | Phase 1 |
| DB schema | ✅ Structured, ready to index | Phase 2 |
| DCB0129 hazard logs | 📋 Excel — requires formalisation via DCB0129 tool | Phase 4 |
| NHS integration contracts | 📋 Requires formalisation | Phase 4 |

---

## Architecture: Resolved Decisions

### Graph query pattern — RAG vs fine-tuning
**Decision:** Graph-as-retrieval-store via MCP tools. No fine-tuning required for initial delivery.  
The agent queries the graph. The graph answers deterministically. Fine-tuning is a separate decision, separate track, explicitly deferred.

### Five indexers
One per source. All run as nightly batch jobs (EC2 Spot). Write unified indices to EFS volume.

| Indexer | Language | Phase |
|---|---|---|
| Roslyn AST indexer | C# | 1 |
| GitHub history indexer | Python | 1 |
| ServiceNow + Confluence indexer | Python | 1 |
| NHS integration surface mapper | Python | 4 |
| Clinical safety indexer (DCB0129) | Python | 4 |

### Five MCP tools
MCP server on ECS Fargate. In-memory cache, refreshed every 5 minutes. Zero DB calls per query. Every response includes `generated_at` timestamp from the index — not the cache. Agent surfaces data age in every reply.

| Tool | Source |
|---|---|
| `get_code_semantics` | Roslyn indexer |
| `get_change_history` | GitHub indexer |
| `get_support_context` | ServiceNow/Confluence indexer |
| `get_integration_surface` | NHS integration mapper |
| `get_safety_context` | Clinical safety indexer |

### Strangler fig boundary
Auto-derived nightly from API gateway traffic logs — not a manually maintained manifest. Routes served by EMIS-X vs EMIS Web are ground truth. Namespaces with traffic to both targets classified as in-flight.  
One-time human mapping required: API routes → code namespaces (route_namespace_map.json). More stable than a sprint-by-sprint manifest.

### Clinical safety — interim protection before Phase 4
Static namespace blocklist hardcoded into the LLM custom instruction from day one. If a class matches a clinically classified namespace prefix, the agent must flag it and require human clinical review before proceeding. Costs nothing. Works independently of the graph.  
**Blocklist namespace prefixes must be confirmed by the CSO before Week 1 — this is not an engineering decision.**  
The blocklist remains as a secondary control even after Phase 4 clinical safety tagging is live.

### Developer workspace connectivity
VS Code / Cursor + GitHub Copilot → mcp-remote stdio bridge → VPC ALB (TLS 1.3 + Okta JWT). MCP server never exposed outside VPC.

### CI/CD gates
Six automated gates on every PR. Gates 1, 3, 4 require graph data — these call a separate lightweight REST endpoint, not the MCP server directly (MCP server is built for IDE/agent queries, not CI gate logic).

| Gate | What it checks |
|---|---|
| 1 | Clinical namespace — adds CSO as required reviewer |
| 2 | Strangler manifest alignment |
| 3 | NHS integration contract — triggers integration test suite |
| 4 | DCB0129 hazard check — PR must reference hazard ID |
| 5 | Co-change coupling alert (advisory, not a merge block) |
| 6 | TDD compliance — test commit must precede feat commit |

---

## DCB0129 Tool Design

**Decided in session. Simple, well-defined, won't be designed under delivery pressure.**

- CSO maintains separate Excel documents per clinical safety area
- CSO is single owner
- Nightly batch import job ingests all files
- One parser handles all files (confirmed consistent structure across areas)
- Upserts into DB schema — one schema per clinical safety area
- Filename is the canonical area identifier (CSO controls mapping by controlling filenames)
- Context graph queries DB via the clinical safety indexer
- MCP `get_safety_context` surfaces hazard IDs, severity, and namespace mappings to the agent

This is Phase 4 work per the v6.0 plan. Design is settled now so it won't block Phase 1 and won't need to be figured out under delivery pressure later.

**Strategic note:** The DCB0129 tool is not just a compliance mechanism. Hazard context baked into every LLM call — requirements, architecture, code — is what makes Genesis AI appropriate for a medical organisation. 30 years of DCB0129 hazard history as graph context is not replicable by any competitor.

---

## Delivery Sequence (v6.0 Plan — 16 Weeks)

| Weeks | Phase | Key deliverable |
|---|---|---|
| 1 | Spike | Roslyn + GitHub indexers run manually against one project. Output quality validated. Decision gate — do not proceed if output has systematic gaps. |
| 2 | Thin layer | Nightly automation live. One developer connected. One real ticket fixed with the graph — not a demo, a real merged PR. |
| 3–4 | Phase 1 scale | Full codebase indexed. All UK engineers connected. Strangler boundary auto-derived. |
| 5–6 | ServiceNow + India centre | ServiceNow indexer live at 70%+ mapping coverage. India centre pilot — three engineers, one familiar module, one week. |
| 7–10 | Phase 2 | DB schema and architecture diagrams ingested. `get_architecture_context` added to MCP. |
| 11–12 | Phase 3a | Confluence ingested for top support areas (Prescribing, GP2GP, Consultations). Business intent available in MCP. |
| 13–16 | Phase 4 | Clinical safety tagger live. First clinical migration sprint under Step 0 safety pre-check protocol. |

---

## Open Questions — AMP v3.0

Ten questions must be answered before AMP v3.0 can be baselined as v1.0. Current status:

| Q | Question | Status |
|---|---|---|
| Q1 | Which complexity dimension is most underdocumented? Where is tribal knowledge most at risk? | ❓ Open — internal |
| Q2 | Does strangler_manifest.json exist anywhere? Who owns it? | ❓ Open — internal (v6.0 solves via traffic logs but log group must be confirmed) |
| Q3 | Is the GitHub org accessible from the VPC? PAT or GHES constraints? | ❓ Open — infra team |
| Q4 | DCB0129 hazard log format? | ✅ Resolved — Excel, one file per clinical safety area, CSO owned, nightly import to DB |
| Q5 | Existing NHS integration catalogue? | ✅ Resolved — nightly CI/CD is the answer |
| Q6 | ServiceNow API access? Authentication mechanism? DBA approval needed? | ❓ Open — internal |
| Q7 | Is Okta the correct identity provider for VPC ALB? | ❓ Open — infra team |
| Q8 | Can GitHub Actions / ADO call MCP server, or separate REST endpoint needed? | ✅ Resolved — separate lightweight REST endpoint for CI gates |
| Q9 | Who can override a Step 0 clinical block — CSO alone or CSO + EM? | ❓ Open — policy decision, not engineering |
| Q10 | Is 10-week timeline realistic? | ✅ Resolved — v6.0 extends to 16 weeks, which is the working plan |

**Remaining internal questions (Q1, Q2, Q6, Q9)** can be answered without waiting for TPG.  
**Infra questions (Q3, Q7)** need the infra team.  
**TPG partner conversation** is about methodology and tooling — not blocked by any of the above.

---

## Agent Quality Feedback Loop

Lightweight PR divergence tracking — not a training loop, a calibration signal.

When a PR merges, a GitHub Actions workflow compares the final diff against what the agent proposed. Divergence scored LOW / MEDIUM / HIGH per namespace. Scores accumulate in the graph. MCP server returns confidence score alongside code semantics. Agent surfaces confidence level in every reply so developers calibrate review effort accordingly.

Start accumulating scores from Week 3. Use to adjust agent behaviour from Week 6 once enough data exists.

---

## Metrics

### Engineering (weekly)
- Time from ticket open to proposed fix — target < 30 minutes by Week 4
- HIGH divergence namespaces — target < 20% of total by Week 10
- Graph freshness violations — zero responses older than 25 hours

### Support operations (weekly)
- ServiceNow mapping coverage — target 85%+ by Week 10
- Stale map entries — zero (alert if non-empty)
- Ticket-to-engineering handoff time — reduction vs baseline by Week 6

### Leadership (monthly)
- Support ticket volume by namespace — informs migration prioritisation
- Average resolution time by product area
- Migration sprint velocity (modules per sprint)
- Clinical PRs requiring CSO review vs total clinical PRs — DCB0129 compliance signal for TPG and NHS England

---

## Next Steps

Workstream C does not start until:
1. Plan 3c (apply_to_scope) complete
2. Plan 3d (output template contracts + feedback loop) complete
3. TPG introductions confirmed for specialist partner (knowledge graph extraction + LLM fine-tuning)

Internal pre-work that can happen in parallel:
- Answer Q1, Q2, Q6, Q9 (no external dependency)
- Confirm API gateway log group exists and is queryable (Q2 / strangler boundary)
- Confirm GitHub org VPC access and CI/CD platform (Q3, Q7, Q8 — infra team)
- CSO to confirm complete list of clinically classified namespace prefixes (blocklist for custom instruction)

---

*Document owner: Idris Issa | Version: 1.0 | Classification: Internal — Confidential*
