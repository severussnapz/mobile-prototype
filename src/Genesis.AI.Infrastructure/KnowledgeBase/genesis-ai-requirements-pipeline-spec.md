# Genesis AI — Requirements Pipeline Specification

**Version:** July 2026  
**Owner:** Idris Issa, CTO Optum UK  
**Scope:** The Genesis AI requirements pipeline — how requirements are captured, structured, approved, and propagated through the pipeline into code.

---

## Overview

The Genesis AI requirements pipeline is a structured AI-assisted workflow that transforms a product conversation into deployed, regulated software. It is not a chat tool. It is an engineering discipline enforced by the tool — every stage produces a defined artefact, every artefact is approved by a human, and every approved artefact is traceable from requirement to test to code.

The pipeline has eleven stages (P01–P11). Each stage is a conversation between a human and a Bedrock-powered agent, constrained by a template contract. The agent cannot advance to the next stage without a human approval gate. Every output is stored as a structured markdown artefact in S3 and committed to `.genesis/` in the feature repo on approval.

---

## Stage Architecture

### How Each Stage Works

Every pipeline stage follows the same execution pattern:

1. **Session start** — the user opens a stage conversation. The agent loads the system prompt for that stage (e.g. `Pipeline01RequirementsDiscovery.md`), the project context from P00, and any approved artefacts from previous stages via `get_artefact`. The conversation never starts cold.

2. **Elicitation** — the agent interviews the user. Each stage has a defined set of mandatory questions and phases. The agent cannot skip mandatory phases or advance past exit gates without the required information.

3. **Output generation** — the agent writes the stage artefact in real time, following the template contract for that stage. The output is structured markdown with defined sections. The TDD agent (Plan 5) will extract test specifications directly from these sections.

4. **Human review** — the user reviews the draft artefact. They can request amendments, which the agent incorporates. The artefact is never final until the human explicitly approves it.

5. **Approval and persistence** — on approval:
   - The artefact is stored in S3 and the DB
   - `genesis-ai[bot]` commits it to `.genesis/{stage-folder}/` in the feature repo
   - The artefact is indexed into the `project-artefact` knowledge namespace in pgvector (Plan 4b)
   - Domain impact badges (CS/IG/SEC) are surfaced if the change affects regulated domains
   - The next stage is unblocked

6. **Session close** — when the user clicks "Close Session", a `SESSION-CLOSE-P0n.md` artefact is generated summarising what was captured, what is open, and where to pick up next time. This is upserted (not duplicated) and injected at the top of context when the session is resumed.

---

## P00 — Project Initialisation

**Not a conversation stage.** P00 is a form filled in once at project creation. It captures foundational project context that flows into every downstream stage automatically — no stage re-elicits this information.

**Fields captured:**
- Release type (EMIS Web / EMIS-X)
- Assurance required (yes/no)
- Pilot/deployment process
- Stakeholders and roles
- Clinical Safety Officer (CSO) — flows into P06
- IG owner — flows into P07
- Security reviewer — flows into P08
- Medical Device classification flag — flows into P09 when designed
- GitHub repo URLs (API repo, App repo) — feeds `genesis-ai[bot]` artefact push

**Output artefact:** `PROJECT.md` — committed to `.genesis/project/` in the feature repo on save.

**Why this matters:** Without P00, every pipeline stage would have to ask the same foundational questions. With P00, the CSO is known before P06 starts. The release type is known before P01 starts. The feature repo URL is known before any artefact is committed. The form is completed once. Everything else inherits.

---

## P01 — Requirements Discovery

**Purpose:** Capture a complete, structured, traceable requirements specification for a single product increment.

**Artefact:** `REQ-{id}.md`

**Structure of REQ file (verified from REQ-001):**

```
# REQ-{id} — {Feature Name}

## Increment Scope
What is in scope for this increment. What is deferred to later increments.

## Business Context
Why this feature exists. What problem it solves. Stakeholder impacts.

## User Personas
Who uses this feature and in what context.

## Requirements
Each requirement structured as:
  - Requirement ID (e.g. DOCMGT-001)
  - Description
  - Acceptance Criteria (minimum 3, formatted as Given/When/Then or structured CHECKs)
  - Clinical Safety implications (HAZ-ID references)
  - IG implications
  - Security implications

## Evaluation Specification (CHECKs)
Each CHECK is a named, executable test scenario:
  - CHECK N: {Name}
  - Setup: {preconditions}
  - Action: {what the user does}
  - Expected: {what happens}
  - Pass criteria: {how to determine pass/fail}

## Hazard Log
HAZ-ID assignments, severity, likelihood, mitigations, guardrails — traced to specific ACs.

## ADRs
Architectural Decision Records for this increment.

## DB Schema
Tables, columns, constraints proposed for this increment.

## Component Interfaces
API contracts, service interfaces proposed for this increment.

## OTEL Spans
Observability requirements — what must be instrumented.

## Traceability Table
Requirement → AC → CHECK → HAZ-ID → Mitigation → Guardrail
```

**Mandatory phases in P01:**
- Phase 0: Business Context (BC) — stakeholder mapping, release type, assurance gate
- Phase 1: User Personas (UP) — who uses this and why
- Phase 2: Non-Functional Requirements (NFR) — performance, scalability, reliability
- Phase 3: Clinical Safety (CS) — lightweight routing anchor only (deep elicitation is P06)
- Phase 4: Information Governance (IG) — lightweight routing anchor only (deep elicitation is P07)
- Phase 5: Security (SEC) — lightweight routing anchor only (deep elicitation is P08)

**Exit gate:** The REQ file must contain:
- Minimum 3 Acceptance Criteria per requirement
- Minimum 2000 characters of content
- Business linkage populated
- All mandatory phases completed

**Why REQ files are the foundation:** The REQ file is the single source of truth for an increment. Every downstream stage (P02–P08) reads from it via `get_artefact`. If the REQ file changes, the change propagates. The TDD agent (Plan 5) extracts test specifications from the CHECK sections. The code swarm (Plan 6) writes code against those tests. The hazard log in P06 traces back to HAZ-IDs in the REQ file. The DB schema in P04 implements the schema proposed in the REQ file. Nothing downstream is disconnected from the REQ file.

---

## P02 — Prototype Demo Builder

**Purpose:** Generate a clickable, styled EMIS-X prototype from the approved REQ file before any production code is written. The prototype validates UX decisions with stakeholders before engineering begins.

**Artefact:** `prototype/index.html`

**How it works:**

The Demo Builder (Plan 4) is a v0/Lovable-style tool — chat-left, preview-right, single self-contained HTML rendered in a sandboxed iframe. The user describes the feature in plain language. The agent generates a complete, styled, clickable prototype anchored on the EMIS-X UI kit.

Three editing modes:
- **Generation** — describe the feature from scratch. Agent generates a complete prototype.
- **Surgical edit** — right-click any element in the preview. Describe the change. The element is replaced server-side using `PrototypeElementReplacer` (fingerprint matching). Deterministic. Not re-generated.
- **Vibe edit** — free-text instruction in the chat. The agent applies the change via `edit_artefact`.

**Key constraints:**
- UI kit: EMIS-X only. No custom CSS authoring.
- The prototype must contain "PROTOTYPE ONLY" text — enforced by a runtime guard.
- Token usage is recorded for both generation and surgical edits.
- File attachments (PNG/JPG/MD/PDF) can be provided as reference material.
- Version history is maintained in S3 — any previous version can be restored.

**Why prototype before code:** The prototype surfaces UX decisions before engineering cost is committed. A BA can validate the flow with a clinical user in a meeting. A PxD lead can check component composition against the EMIS-X design system. Issues found at prototype stage cost minutes to fix. Issues found at code review cost days.

---

## P03 — Architecture

**Purpose:** Define the system architecture for the increment — service boundaries, integration patterns, data flows, and key ADRs.

**Artefact:** `ARCH-{id}.md`

**Feeds from:** REQ file (via `get_artefact`). The architecture agent reads the REQ file's DB schema, component interfaces, and ADR sections as starting context. It does not start from scratch.

**Key content:**
- Service decomposition
- API contract design
- Integration patterns (synchronous/asynchronous, event-driven)
- Data flow diagrams
- Architecture Decision Records (ADRs) with rationale
- Technology stack decisions
- Non-functional architecture (resilience, observability, security posture)

**Relationship to REQ:** The architecture must implement every interface proposed in the REQ file. Any architecture decision that changes a REQ-level DB schema or component interface triggers a requirement change (CHANGE record) back to P01.

---

## P04 — Design (API/DB)

**Purpose:** Produce detailed API contracts and database schema design for the increment.

**Artefact:** `ARCH-{id}-design.md` (or appended to the ARCH artefact)

**Key content:**
- OpenAPI/REST contract per endpoint
- EF Core entity definitions
- Flyway migration SQL
- Index strategy
- Constraint definitions

**Feeds from:** REQ file (DB schema section) + ARCH artefact (service decomposition).

---

## P05 — PxD (Product Experience and Design)

**Purpose:** Produce detailed UI/UX design specifications aligned to the EMIS-X design system.

**Artefact:** `PXD-{id}.md`

**Key content:**
- Component composition (which EMIS-X components are used)
- Interaction patterns
- Error states and empty states
- Accessibility requirements (WCAG 2.1 AA)
- Responsive behaviour

**Feeds from:** REQ file + prototype artefact (via `get_artefact`). The PxD agent reads the prototype HTML to understand what has already been validated with stakeholders.

---

## P06 — Clinical Safety (DCB0129)

**Purpose:** Produce a complete DCB0129 Clinical Safety Case for the increment, including hazard identification, risk assessment, mitigations, and guardrails.

**Artefact:** `DCB0129-{id}.md` + `DCB0129-{id}.xlsx`

**On approval:**
- The MD artefact is committed to `.genesis/clinical-safety/` in the feature repo
- The XLSX export is generated in the CSO's existing Excel schema
- The CS team's hazard tracking DB is updated via API integration (Plan 4c)

**Key content:**
- Hazard identification (HAZ-ID referenced from REQ file)
- Severity and likelihood scoring (per DCB0129)
- Risk level (ALARP assessment)
- Control measures
- Residual risk
- Guardrails (CHECKs that validate clinical safety in code)
- CSO sign-off record

**Feeds from:** REQ file HAZ-ID entries + P03 architecture (system context). The clinical safety agent does not start from scratch — it reads every HAZ-ID in the REQ file and builds the hazard log from them.

**Governance:** P06 prompt changes require approval from `@emisgroup/clinical-safety-owners` via GitHub PR. Indra Joshi must be involved before any prompt changes are made.

**Why not just a document:** The DCB0129 artefact is a DCB0129 submission document. It must be complete, traceable, and signed off by the CSO before the feature goes to production. The hazard workshop is still required — P06 changes the workshop from a blank-sheet exercise to a review-and-challenge session against a structured, pre-populated document.

---

## P07 — Information Governance / DPIA

**Purpose:** Produce a Data Protection Impact Assessment (DPIA) and IG compliance record for the increment.

**Artefact:** `IG-{id}.md`

**Key content:**
- Data flows (what data is processed, where it goes, how long it is retained)
- Lawful basis for processing (UK GDPR Article 9 for special category health data)
- Data minimisation assessment
- Retention and deletion policy
- Third-party data sharing assessment
- DPIA conclusion

**Feeds from:** REQ file + P03 architecture.

**Governance:** P07 prompt changes require approval from `@emisgroup/ig-owners`.

---

## P08 — Security

**Purpose:** Produce a security review covering OWASP ASVS controls, threat modelling, and security architecture decisions for the increment.

**Artefact:** `SEC-{id}.md` + `SEC-{id}.xlsx` (security review workbook)

**Key content:**
- Threat modelling (STRIDE)
- OWASP ASVS control mapping
- Attack vector coverage
- Security ADRs
- Penetration test scope recommendation

**Feeds from:** REQ file + P03 architecture.

**Governance:** P08 prompt changes require approval from `@emisgroup/security-owners`.

---

## P09 — Medical Device (MDR/MHRA) [PLANNED]

**Status:** Not yet built. Requires Indra Joshi's sign-off on which EMIS-X features qualify for UK MDR 2002 / MHRA classification before any engineering work begins.

**When built:** Will produce a Medical Device technical file, intended purpose statement, risk classification, and MHRA registration evidence. Sits alongside P06 Clinical Safety — not after P08 Security.

---

## P10 — Pre-Swarm Decision Gate

**Purpose:** Surface all consolidated product decisions from every approved pipeline stage (P01–P08) for final human confirmation before code generation begins.

**This is not a conversation.** It is a review gate — the agent reads all approved artefacts for the project, extracts every decision that has been made (architectural, clinical, IG, security, UX), presents them in a consolidated summary, and requires explicit human sign-off before P11 begins.

**Why this exists:** Code generation (P11) is expensive to reverse. By the time the swarm writes code, every product decision must be locked. This gate prevents "the CS team approved the hazard log but didn't realise what architectural pattern was chosen" failures.

---

## P11 — TDD / Code Generation

**Purpose:** Generate a complete, tested, production-ready implementation of the approved increment.

**Two-agent architecture (Plan 5):**
- **Agent A (TDD):** Reads the REQ file CHECK sections and generates a failing test suite. Cannot see the implementation. Tests are written to the acceptance criteria and evaluation specifications in the REQ file.
- **Agent B (Coding):** Reads the capability catalogue, API contracts, and architecture artefacts. Writes code to make the failing tests pass. Cannot modify tests.

**The test suite is the shared collision point** — Agent A retrieves from requirements, Agent B retrieves from capability catalogue. Both are validated simultaneously by whether the tests pass.

**Review Agent:** After each wave, a Review Agent (the "bouncer") reviews outputs against clinical and architectural standards with no emotional attachment to authorship. Yes/No/Redo — never subjective.

---

## Change Management

### How Requirement Changes Work

When a requirement changes after P01 is approved — either through a new finding in P06, a technical constraint identified in P03, or a stakeholder change request — the Genesis AI pipeline handles it via the `propose_requirement_change` tool.

**The CHANGE record pattern:**

1. The agent (at any pipeline stage) proposes a change using `propose_requirement_change`
2. The change is classified with:
   - **Change type:** addition, modification, deletion
   - **Impact level:** low, medium, high
   - **Domain impact badges:** CS (Clinical Safety), IG (Information Governance), SEC (Security) — any badge marks the downstream stage as requiring re-review
3. The change is stored as a `CHANGE-{id}.md` record alongside the REQ file in S3 and `.genesis/requirements/`
4. The human reviews and approves or rejects the change
5. On approval: the REQ file is amended, the CHANGE record is committed to the feature repo, and domain impact badges trigger downstream stage re-review notifications

**The GAP / CLARIFICATION / CONTRADICTION classification:**

During any pipeline conversation, the agent classifies its own uncertainty:
- **GAP:** Information is missing that is required to complete the artefact
- **CLARIFICATION:** Information exists but is ambiguous — the agent needs the human to confirm interpretation
- **CONTRADICTION:** Two pieces of approved information conflict with each other

The agent surfaces these explicitly rather than making assumptions. The human resolves them before the artefact is approved.

**Cross-pipeline change propagation:**

When a REQ file changes, domain impact badges determine which downstream stages are affected. A CS badge on a requirement change means P06 must re-review. An IG badge means P07 must re-review. This is not manual — the pipeline tracks which stages have been approved and flags which ones need to be revisited given the change.

---

## Session Continuity

### How Sessions Are Resumed

Every pipeline stage maintains session continuity across browser sessions and multi-day work:

**Conversation persistence:** Chat history is stored in the DB. When a user returns to a stage conversation, the full history is restored. The agent resumes from where it left off.

**Artefact persistence:** Every approved artefact is in S3, indexed in pgvector, and committed to `.genesis/` in the feature repo. The agent reads the current artefact state at the start of every session via `get_artefact`.

**SESSION-CLOSE artefact:** When a user clicks "Close Session", a `SESSION-CLOSE-P0n.md` artefact is generated. It summarises:
- What was captured in this session
- What is open / in the parking lot
- Where to pick up next time (one-line entry point for the next session)

On the next session start, the `SESSION-CLOSE-P0n.md` is injected at the top of context before the first turn. The agent knows exactly where to resume.

**ContinuedFromConversationId:** When a session hits the tool-use limit, a new conversation is created and linked to the previous one via `ContinuedFromConversationId`. The handover context is injected automatically. The user experiences continuity.

---

## The Knowledge Layer (Plan 4b)

### How the Help Chat Knows About the Project

The Genesis AI Knowledge Service (built in Plan 4b) gives every conversation access to two layers of knowledge:

**Layer 1 — Genesis AI tool knowledge (`genesis-tool` namespace):**
Every pipeline prompt file (P01–P10), every skill file (115 skills), every policy document, and every project knowledge document (master plan, workstream designs, architectural decisions, coding standards) is indexed into pgvector at deployment time via the `KnowledgeSeederService`.

When a user asks "what does P06 do?" or "how should I handle a CONTRADICTION response?", the help chat retrieves the most relevant chunks from this namespace and answers from the actual pipeline documentation.

**Layer 2 — Project artefacts (`project-artefact` namespace):**
Every approved artefact — REQ files, ARCH documents, DCB0129 hazard logs, IG records, security reviews, prototypes — is indexed into pgvector at approval time via the `ArtefactPublishedInterceptor`. Tagged with `projectId` so queries are always scoped to the current project.

When a user asks "what requirements did we capture for patient matching?" or "what hazards have been identified?", the help chat retrieves the relevant chunks from the actual approved artefacts for this project.

**Workstream C plug-in:** When the full Knowledge Graph Service delivers (Darren's team), the `IKnowledgeService` call is swapped for a Knowledge Graph MCP call. The help chat gains cross-project patterns, blast radius analysis, and codebase context. No changes to the frontend or the API controller. One DI registration swap.

---

## Artefact Traceability

### The Full Chain

Every feature delivered through Genesis AI has a complete, auditable traceability chain from business conversation to deployed code:

```
P00 PROJECT.md
  → P01 REQ-{id}.md (requirements, ACs, CHECKs, HAZ-IDs, ADRs, DB schema)
    → P02 prototype/index.html (validated UX)
    → P03 ARCH-{id}.md (architecture, ADRs, integration patterns)
    → P04 design (API contracts, migrations)
    → P05 PXD-{id}.md (component composition, interaction patterns)
    → P06 DCB0129-{id}.md (hazard log, ALARP, controls)
    → P07 IG-{id}.md (DPIA, lawful basis, retention)
    → P08 SEC-{id}.md (ASVS controls, threat model)
    → P10 Pre-Swarm Decision Gate (consolidated review)
    → P11 Test suite (from REQ CHECKs)
    → P11 Production code (from test suite)
```

Every artefact in this chain is:
- Stored in S3 and the DB (runtime access)
- Committed to `.genesis/` in the feature repo (audit trail, Git history)
- Indexed in pgvector (help chat knowledge)
- Attributed to the approving user (RBAC + CODEOWNERS audit trail)

If a regulator asks "show me the clinical safety case for the inbox feature, who approved it, and when" — the answer is: `DCB0129-REQ001.md` in `.genesis/clinical-safety/` of `emis-x-document-manager`, committed by `genesis-ai[bot]` on behalf of the CSO, timestamped in Git, attributed in the commit message, with every version in Git history.

---

## What Genesis AI Is Not

- **Not a documentation tool.** The artefacts are not documents written after the fact. They are the live specification from which code is generated. They are written before code, not after.
- **Not a Copilot prompt.** Every pipeline stage is a structured interview engine with mandatory phases, exit gates, template contracts, and human review gates. The raw Copilot prompt experience — no structure, no persistence, no traceability — is exactly what Genesis AI replaces.
- **Not a replacement for human judgement.** The clinical safety workshop is still required. The architecture review is still required. The security assessment is still required. Genesis AI changes what those sessions look like — from blank-sheet exercises to review-and-challenge sessions against a structured, pre-populated artefact.
- **Not a one-size-fits-all tool.** Every pipeline stage is parameterised per project via P00. The release type, the CSO, the IG owner, the security reviewer — all flow into the pipeline from project initialisation. The tool adapts to the project, not the other way round.

