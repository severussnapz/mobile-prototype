# Genesis AI — Getting Started Guide

## What is Genesis AI?

Genesis AI is the AI pipeline that powers the EMIS Web to EMIS-X migration. It takes a feature — something that exists in EMIS Web — and walks it through every stage needed to migrate it safely: requirements, prototype, architecture, clinical safety, information governance, security, and code generation.

Every output is traceable. Every decision is versioned. Nothing leaves the VPC.

You do not need to understand how it works under the hood to use it. This guide will get you from zero to your first artefact.

---

## The pipeline at a glance

Genesis AI has eight active stages. They run in sequence for each feature you are migrating.

| Stage | What it produces | Who owns the session |
|-------|-----------------|----------------------|
| P01 — Requirements Discovery | REQ-*.md files — user stories and acceptance criteria | BA / Product |
| P02 — Prototype Demo Builder | A clickable HTML prototype for stakeholder validation | BA / Product |
| P03 — Architecture | ARCH-*.md — components, APIs, data models | Tech Lead |
| P04 — Design (API/DB) | API contracts and database schema | Tech Lead / Engineer |
| P05 — PxD | Product experience design review | PxD / Design |
| P06 — Clinical Safety | DCB0129 hazard log and safety case | Clinical Safety Officer |
| P07 — Information Governance | DPIA and data flow documentation | IG Owner |
| P08 — Security | Threat model and security review | Security Reviewer |

You do not need to complete all stages before starting the next. Requirements (P01) and Prototype (P02) typically run together. Architecture and regulated stages (P06, P07, P08) follow once requirements are approved.

---

## Step 1 — Create a project

Every migration feature needs a project. A project holds all the pipeline stages, artefacts, conversations, and settings for one feature.

1. Go to the Genesis AI home page and click **New Project**.
2. Enter the feature name — for example, "Unified Inbound Document Inbox".
3. Enter a description — one sentence describing what the feature does.
4. Set the compliance domain — this is the clinical area (e.g. GP Clinical, Pharmacy, Documents).
5. Click **Create**.

Your project is now set up. You will land on the project detail page showing all pipeline stages.

---

## Step 2 — Complete the project settings (P00)

Before starting any pipeline stage, complete the project settings. These fields are captured once and flow automatically into every stage — no stage will ask you for them again.

Go to the **Settings** tab on your project page. You will see three sections:

**Project Details:**
- Name, description, and timesheet code — pre-filled from project creation

**GitHub Configuration:**
- API Repo URL — the GitHub repository for the API component of this feature (e.g. `https://github.com/emisgroup/emis-x-documents-api`)
- App Repo URL — the GitHub repository for the frontend component (optional)
- When you save, Genesis AI verifies the connection automatically. A green banner means the connection is working. A red banner means the URL is wrong or genesis-ai-bot does not have access.

**P00 Project Configuration:**
- Release type — EMIS Web or EMIS-X
- Assurance required — yes or no (determines which regulated stages are mandatory)
- Pilot / deployment process — how this feature will be deployed
- Clinical Safety Officer role — the role responsible for P06 sign-off
- IG Owner role — the role responsible for P07 sign-off
- Security Reviewer role — the role responsible for P08 sign-off
- Medical Device flag — whether this feature has Medical Device implications (determines if P09 is required)

Save each section separately. The settings are stored against the project and committed to the feature repository as `PROJECT.md`.

---

## Step 3 — Start Requirements Discovery (P01)

P01 is where you describe the feature to Genesis AI. The agent reads your input, asks clarifying questions, and produces structured requirement files (REQ-*.md) covering user stories, acceptance criteria, and known gaps.

1. On the project page, find the P01 — Requirements Discovery stage card and click **Start**.
2. A conversation panel opens. Describe the feature you are migrating.
3. The agent will ask clarifying questions — typically about user roles, edge cases, and NHS integration points. Answer them directly and specifically.
4. The agent will produce REQ-001.md as the first output. Review it in the Artefacts panel.
5. Continue the conversation to refine or add requirements. Each new requirement gets its own REQ file.
6. When you are satisfied, click **Approve** on each artefact. Approved artefacts are committed to the feature repository automatically.

**What makes a good P01 session:** See the prompt quality guide for examples. The short version: give the agent ground truth — screen names, workflow steps, NHS message formats, field names, user roles — not adjectives.

**Session Close:** When your P01 session is complete, click **Close Session**. This generates a SESSION-CLOSE-P01.md artefact summarising what was covered, what decisions were made, and what is parked for later. This artefact is committed to the repository as part of the audit trail.

---

## Step 4 — Build a Prototype (P02)

P02 is the Prototype Demo Builder. It generates a clickable HTML prototype based on your approved requirements. You use this to validate the requirements with stakeholders before any engineering begins.

1. Click **Open Demo Builder** on the P02 stage card.
2. The builder opens with a chat panel on the left and a live preview on the right.
3. Type your instructions in the input box and press Enter or click **Start Over**.

### First build

If this is your first build, type a brief describing what you want:

> "Build a prototype for the unified inbound inbox from REQ-001 and REQ-002. Priority flows: GP triages an unmatched document, GP files a matched document. List view on the left, document preview on the right."

The agent reads your approved requirements and builds a prototype. It may ask a few clarifying questions before generating. Answer them, then wait for the preview to appear.

### Making changes

**Small changes** — type your instruction in the input box and press Enter. The agent edits the existing prototype. Example: "Add a dropdown filter for Document Type with options: All Types, Referral, Lab Result, Clinic Letter."

**Precise changes** — right-click on any element in the preview, type your instruction, and click Apply. The agent edits only that element. Example: right-click on the File button → "Change the label to 'File to Care Record'."

**Large changes** — type new instructions in the input box and click **Start Over**. The agent rebuilds from scratch using your instructions. The previous version is always saved — use **Recover Version** if the rebuild is worse.

### Approving the prototype

When the prototype is ready for stakeholders, click **Approve** on the prototype artefact. It is committed to the feature repository as `prototype/index.html`.

---

## Step 5 — Regulated stages (P03, P06, P07, P08)

The regulated stages follow the same pattern as P01:

1. Click **Start** on the stage card
2. The agent reads your approved requirements and previous stage artefacts automatically — you do not need to paste them in
3. Provide any additional context the agent needs (see the prompt quality guide for each stage)
4. Review and approve the artefact
5. Close the session

**P03 — Architecture:** The agent produces an architecture document covering components, APIs, data models, and failure modes. Tell the agent about constraints that are not in the requirements — existing services that must be reused, infrastructure boundaries, NHS integration points, and non-functional requirements (latency, availability, scale). The agent reads your approved requirements automatically but cannot infer what you are not allowed to build from scratch.

**P04 — Design (API/DB):** The agent produces the API contracts and database schema for the feature. Before starting P04, confirm with your tech lead which API endpoints are needed and whether any existing endpoints can be extended. Tell the agent the naming conventions used in the codebase, the authentication model (JWT, session, API key), and any field-level constraints (e.g. NHS number format, SNOMED code validation). The more specific you are about field names and types, the less rework the engineering team will need.

**P05 — PxD (Product Experience Design):** The agent produces a product experience design review covering user flows, accessibility, and design decisions. This stage is owned by the PxD or Design team. Bring your approved prototype artefact into the conversation — the agent will use it as the reference for the design review. Flag any known accessibility constraints (screen reader requirements, keyboard navigation, colour contrast standards) and any EMIS design system components that are not yet available in EMIS-X.

**P06 — Clinical Safety:** The agent produces a DCB0129 hazard log. Your Clinical Safety Officer must review and approve this artefact. Tell the agent about specific hazard scenarios relevant to your feature — do not rely on it to infer them from the requirements alone. Example: "The key hazard is a document filed to the wrong patient due to a false-positive name and DOB match. The existing mitigating control is NHS number mandatory for filing. Assess residual risk." The agent will not know about mitigating controls that exist in EMIS Web unless you tell it.

**P07 — Information Governance:** The agent produces a DPIA. Tell the agent about the data types involved, external senders, retention periods, and access controls. These are not always in the requirements. Example: "Data processed: NHS number, DOB, full name, document content. External sender: Docman Connect via HL7 v2. Retention: 10 years per GP records schedule. Access: GP and designated reception staff only."

**P08 — Security:** The agent produces a threat model and security review workbook. Tell the agent about authentication, authorisation, external integrations, and data in transit. Specifically: how users authenticate to this feature, whether any external APIs are called (and whether they are NHS-internal or internet-facing), what data is stored and where, and whether any data crosses a trust boundary. The agent reads your architecture artefact automatically but security-specific context — API keys, token lifetimes, rate limiting decisions — must be provided by you.

---

## Artefacts — what happens to them

Every artefact you approve is:

1. Stored in Genesis AI (S3 + database)
2. Committed by `genesis-ai[bot]` to the `.genesis/` folder in your feature repository
3. Indexed nightly by the Knowledge Graph Service

The `.genesis/` folder structure in your feature repository:

```
.genesis/
  requirements/         REQ-001.md, REQ-002.md, CHANGE-*.md
  architecture/         ARCH-001.md
  clinical-safety/      DCB0129-001.md, DCB0129-001.xlsx
  ig/                   IG-001.md
  security/             SEC-001.md
  prototype/            index.html
  session-close/        SESSION-CLOSE-P01.md … SESSION-CLOSE-P08.md
  project/              PROJECT.md
```

This is the living knowledge base for your feature. Every engineer working on the feature can read exactly what was decided, why, and when.

---

## Common questions

**Do I have to complete stages in order?**
P01 before P02 — you need requirements before prototyping. P03 onwards can be started once P01 requirements are approved. The regulated stages (P06, P07, P08) can run in parallel with P03/P04 once you have approved requirements.

**Can I go back and add more requirements after starting P02?**
Yes. P01 stays open. Add more requirements at any time. The prototype builder has access to all approved artefacts — it will see the new requirements the next time you build or rebuild.

**What if the agent produces something wrong?**
Do not approve it. Continue the conversation to correct it. If the artefact is substantially wrong, type specific corrections — "REQ-001 says the user must select a SNOMED code before filing, but the acceptance criteria do not cover what happens when no SNOMED codes match the document type. Add that as a gap." The agent will revise.

**What is the Parking Lot?**
Items that came up during the session but cannot be resolved in the current context. The agent adds things here automatically — for example, a design decision that needs a stakeholder call, or a requirement that depends on a third-party API spec not yet available. Check the Parking Lot at the end of each session and action the items.

**What does Close Session do?**
It generates a session summary (SESSION-CLOSE-P0x.md) that captures what was covered, key decisions, and open items. This is committed to the repository. It is important for audit trail — future sessions and future engineers can see exactly what happened in each session.

**Can I see how much the AI is costing?**
Yes. The **Usage** tab on the project page shows token usage and cost per pipeline stage. Every generation, edit, and conversation is tracked.

**What if genesis-ai-bot cannot push to my repository?**
Check the GitHub Configuration section in Project Settings. Save the repository URLs — Genesis AI verifies the connection when you save. A red banner tells you exactly what the problem is (repository not found, access denied, etc.). Make sure genesis-ai-bot has been installed on the repository via the GitHub App settings.

---

## Getting help

The **Help** panel is available from any pipeline stage. Click the Help icon to open a project-aware assistant. You can ask it anything about Genesis AI, the current pipeline stage, or your project artefacts.

Examples:
- "What did we decide about patient matching in P01?"
- "How do I write a good P06 hazard scenario?"
- "What is the difference between a gap and a clarification?"
- "Show me the REQ-001 acceptance criteria."

The Help assistant has access to all your approved artefacts and the full Genesis AI documentation.
