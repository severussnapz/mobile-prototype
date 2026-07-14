# Genesis AI — Product Engineering Intelligence
## End State Vision & Working Backwards Plan

---

## The Vision

EMIS Web to EMIS-X is a complex migration.

Not because of scale alone. Because of constraints:

- 35 million patients — data integrity non-negotiable
- 3,500+ practices — zero disruption to clinical care
- DCB0129 / DCB0160 — clinical safety compliance at every step
- NHS data standards — SNOMED, FHIR, HL7, dm+d
- NHS integrations — Spine, eRS, EPS, GP2GP, MESH
- NHS contracts — NPfIT obligations, GP IT framework
- 25 years of clinical workflow — understood, replicated, improved

Genesis AI is how we execute that migration — faster, safer, and with complete traceability from EMIS Web behaviour to EMIS-X capability.

---

## What Genesis AI Does

**It is a product engineering intelligence built around the EMIS Web to EMIS-X migration.**

The context graph is EMIS Web. Every screen, every workflow, every data model, every clinical rule, every NHS integration dependency — captured once, used forever.

When a capability moves from EMIS Web to EMIS-X, Genesis AI already knows what it does, what the safety implications are, what the data dependencies are, and what correct looks like.

**The pipeline:**

**1. Requirements Capture**
Derived from observed EMIS Web behaviour and customer input — not written from scratch. Product person speaks with a GP practice. Genesis AI listens, structures requirements in real time, identifies gaps against what EMIS Web already does.

**2. Prototype Generation**
A clickable prototype generated from approved requirements in minutes. The customer interacts with it live. Requirements and prototype stay in sync automatically through a formal feedback loop.

**3. Prototype Feedback Loop**
Every customer interaction either edits the prototype directly or updates requirements — classified as GAP, CLARIFICATION, or CONTRADICTION. Every change is auditable. Nothing changes without approval.

**4. Pipeline Completion — by Exception**
Architecture, clinical safety (DCB0129), and information governance generated from approved requirements — by exception against what already exists in EMIS Web. Not from scratch. From 25 years of knowledge.

**5. TDD-First Code Generation**
Test suite generated from all approved documents — behavioural tests from acceptance criteria, safety tests from DCB0129 hazards, compliance tests from IG controls. Code written by an AI swarm against those tests.

**6. Deployed and Validated**
Real code. Regulated. Traceable from customer conversation to production line. Deployed at practice sites for real-world validation.

---

## The Compounding Effect

Every EMIS Web capability migrated:
- Enriches the context graph
- Makes the next migration faster
- Makes the safety analysis more complete
- Brings EMIS-X closer to feature parity

The investment compounds every sprint. Nothing is wasted. Every pound spent is traceable to a workstream, delivers measurable output, and becomes the foundation for the next capability.

This is not a side project. This is how EMIS Web becomes EMIS-X.

---

## Two Modes

**Mode 1 — Without Context Graph**
Starting from scratch. Works for new capabilities, customer-specific requirements, internal rapid prototyping, and POCs where no existing context applies.

**Mode 2 — With Context Graph**
EMIS Web knowledge trained into the model. Building a patient list? The context graph knows EMIS Web already has one — use it as the basis. Architecture, safety and IG produced by exception in hours not days. Works for any product type — new or existing.

---

## Parallel Workstreams

Six workstreams running in parallel, each delivering value independently:

**A — Genesis AI Core** *(live now)*
Requirements, prototype, clinical safety and IG pipelines. Delivering today.

**B — Prototype Reliability** *(Weeks 1–4)*
Reliable bulk editing, template contracts, formal feedback loop. First internal pilot ready.

**C — Context Graph** *(Month 2–3)*
EMIS Web knowledge captured — Roslyn AST, GitHub history, DB schema, DCB0129 logs, NHS integration contracts. Architecture and safety by exception.

**D — TDD Agent** *(Month 3–4, depends on B)*
Test suite generated automatically from all pipeline documents. No test writing overhead.

**E — Code Swarm** *(Month 4–5, depends on C + D)*
First capability from customer conversation to deployed code in one week.

**F — AI Platform & Infrastructure** *(starts now, informs all)*
Local open-source vs frontier model evaluation. Fine-tuning vs RAG. Genesis CLI and IDE integration. Hardware capital vs OpEx. NHS data sovereignty. Guardrails design. This workstream gates every other decision.

---

## What This Means for EMIS-X

Every sprint Genesis AI runs, one more EMIS Web capability is understood, migrated, and validated. The migration accelerates because engineers stop reverse-engineering legacy code and start building on captured knowledge.

The external story is straightforward: we built the tool that executes the EMIS Web to EMIS-X migration. The intelligence built in doing so becomes a platform capability available internally and, in time, to other NHS suppliers facing similar challenges.

---

## Investment Thesis

Every pound spent on Genesis AI:
- Is traceable to a named workstream
- Delivers measurable output in that sprint
- Compounds into the next sprint's capability
- Accelerates the EMIS-X migration directly

The metric that matters is velocity compounding — how fast can we migrate the next EMIS Web capability compared to the last? That number should trend down every sprint as the context graph grows.

### Headcount Required

```
Now:      Idris + coding agents
Month 2:  +2 senior engineers (AI-native)
Month 4:  +1 ML engineer (fine-tuning + infrastructure)
Month 6:  +1 product person (customer sessions + pilot)
Month 8:  +1 clinical/regulatory specialist
Month 12: Review and scale
```

### Ask to TPG

We need introductions to firms with proven capability in two specific areas:

1. Knowledge graph extraction from complex legacy enterprise systems
   at scale — not document mining, actual software and domain
   workflow extraction into structured, queryable graphs

2. LLM fine-tuning on domain-specific knowledge graphs for
   production use — not RAG prototypes, fine-tuned models
   running in regulated environments with full knowledge transfer

TPG portfolio spans healthcare, software and regulated industries
globally. The right introduction here directly accelerates the
EMIS-X migration and compounds the value of the platform.

---

*Document version: 2.0 | Owner: Idris Issa | Classification: Internal Strategy*
