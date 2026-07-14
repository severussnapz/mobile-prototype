# Genesis AI — Module 0: Foundation
## All Roles | Est. 30 minutes

---

## What This Module Covers

This module is for everyone — BA, Architect, PxD, CSO, IG Owner, Security Reviewer, Engineer. Before you run your first pipeline session, you need to understand what Genesis AI is, how it thinks, and what it expects from you.

Complete this module before any role-specific module.

---

## What Genesis AI Is

Genesis AI is not a chatbot. It is not a document generator. It is a **structured engineering intelligence** that turns a product conversation into deployed, regulated software.

Here is what that means in practice:

Every feature you deliver through Genesis AI follows the same path:

```
Customer conversation
  → Requirements capture (P01)
  → Clickable prototype (P02)
  → Architecture (P03/P04)
  → Design (P05)
  → Clinical Safety (P06)
  → Information Governance (P07)
  → Security (P08)
  → TDD + Code Generation (P11)
  → Deployed to practice sites
```

Every stage produces a structured artefact. Every artefact is approved by a human. Every approved artefact is stored in S3, committed to Git, and indexed so that every later stage can read it.

**Nothing in Genesis AI starts cold.** When you open P06, the agent has already read the REQ file, the architecture decisions, and the prototype. You are reviewing, challenging, and signing off — not explaining from scratch.

---

## What Genesis AI Is Not

- **Not a shortcut.** Clinical safety sign-off still requires a trained CSO. Architecture still requires an architect. Genesis AI accelerates the work — it does not replace the accountability.
- **Not infallible.** The LLM will miss things. It will misclassify hazards. It will make assumptions. Your job is to challenge it, not to accept its first output.
- **Not a document generator.** The artefacts are the specification from which tests and code are generated. If they are wrong, the code will be wrong.

---

## The Ground Truth Principle

This is the most important concept in Genesis AI.

The LLM reasons **against** the information you give it — not against what it was trained on. If you give it accurate, specific, grounded information, it produces accurate, specific, grounded output. If you give it vague prompts, it fills the gaps with plausible-sounding invention.

**Ground truth in Genesis AI means:**
- The EMIS Web behaviour you have observed in the running system
- The DCB0129 hazards that have already been filed in previous increments
- The architectural decisions already taken in approved artefacts
- The API contracts already published by upstream services
- The UI patterns already established in the EMIS-X design system

**What the LLM does with ground truth:**
It reasons against it. It does not invent. If you tell it "the existing EMIS Web inbox shows documents by received date descending, with unread items bolded", it will use that as the baseline and reason about what the EMIS-X equivalent should look like. If you do not tell it, it will invent a plausible inbox design that may be completely wrong.

**Your job:** bring the ground truth. The LLM brings the structure, the questions, and the output format.

---

## How Prompting Works in Genesis AI

Genesis AI uses structured prompts — not open-ended conversation. Each pipeline stage has a defined interview engine that asks mandatory questions in a defined sequence.

But the quality of the output depends on the quality of what you put in. Here are the rules:

### Rule 1: Be specific about observed behaviour
❌ "The inbox should show documents"
✅ "The current EMIS Web inbox shows all inbound documents sorted by received date descending. Unread items are shown in bold. The user can filter by document type using a dropdown. There is no search functionality."

### Rule 2: Bring constraints, not wishes
❌ "It should be fast"
✅ "The inbox must load within 2 seconds at P95 under the typical load of 35 patients per session per GP. The existing EMIS Web inbox takes 4–6 seconds — this is a known pain point."

### Rule 3: Name the clinical context explicitly
❌ "Documents can be sensitive"
✅ "Some documents contain HIV status, mental health diagnoses, and safeguarding flags. These must respect the existing EMIS Web confidentiality model — patients can request restricted access by specific staff members."

### Rule 4: Respond to GAP, CLARIFICATION, CONTRADICTION directly
When the agent signals one of these, do not deflect:
- **GAP** — information you need to provide. Go and find it. Do not proceed until you can answer it.
- **CLARIFICATION** — the agent has interpreted something ambiguously. Tell it which interpretation is correct.
- **CONTRADICTION** — two pieces of information conflict. Resolve the conflict before approving.

### Rule 5: Challenge the output before approving
Every artefact has a "Review before approving" checklist. Use it. The agent is not a single source of truth — you are.

---

## Exercise 1: Open the Test Project

**The test project:** GP Appointment Reminder Notifications — a feature that sends automated SMS/email reminders to patients 48 hours before a GP appointment, with patient opt-out.

1. Open Genesis AI in your browser
2. Navigate to Projects
3. Open "GP Appointment Reminders (Training)"
4. Click on the Artefacts tab
5. Open `requirements/REQ-001-appointment-reminders.md`
6. Read the full REQ file

**What to notice:**
- The structure: Business Context, User Personas, Requirements, Acceptance Criteria, CHECKs, Hazard Log, ADRs
- Every requirement has a minimum of 3 ACs
- Every AC that touches patient safety has a HAZ-ID
- The CHECKs are named, executable scenarios — not vague descriptions

**Question to answer before moving on:** Can you identify which requirement is most likely to trigger a clinical safety concern and why?

---

## Exercise 2: Use the Help Chat

1. Click the `?` button in the bottom-right corner of any page
2. Ask: "what does P01 do?"
3. Ask: "what is a CHECK in a REQ file?"
4. Navigate to the test project and ask: "what is the status of the GP Appointment Reminders project?"

**What to notice:**
- When you are inside a project, the help chat has access to the project's approved artefacts
- When you are outside a project, it only has access to Genesis AI tool knowledge
- The help chat does not hallucinate artefact content — if the information is not in an approved artefact, it will say so

---

## The Artefact Contract

Every pipeline stage produces exactly one artefact type. The artefact follows a defined template. The template defines:
- Which sections are mandatory
- What format each section must be in
- What quality gates must pass before approval is possible

**You cannot approve an incomplete artefact.** The system will tell you what is missing.

**After approval:**
1. The artefact is stored in S3 (the live version)
2. A record is created in the database
3. The artefact is indexed into the knowledge base — the help chat can now answer questions about it
4. The next pipeline stage is unblocked

> **Coming in Plan 4c:** On approval, `genesis-ai[bot]` will also commit the artefact to `.genesis/` in the feature repo. Every approved version will be a Git commit. Every amendment will be a diff. Full, immutable audit trail.

---

## Session Continuity

You will not finish a pipeline stage in one sitting. Genesis AI knows this.

**What persists between sessions:**
- Your full conversation history
- Every approved artefact
- The parking lot (items flagged for later)
- Notes and decisions recorded during the session

**How to end a session cleanly:**
Click "Close Session" before you leave. This generates a `SESSION-CLOSE-P0n.md` artefact that summarises what was captured, what is open, and exactly where to pick up next time.

**How to resume:**
Open the conversation for the relevant stage. The agent reads the SESSION-CLOSE artefact and resumes from where you left off. You do not need to re-explain anything.

---

## What Happens When Something Goes Wrong

**If the agent produces something factually wrong:** Challenge it immediately. Say "that is incorrect — the actual behaviour is X". The agent will revise.

**If you approve something by mistake:** Go to the artefact, click Edit, make the correction, and re-approve. The amendment is tracked. The previous version is preserved in S3.

**If a downstream stage raises a conflict with an earlier decision:** Use `propose_requirement_change`. This creates a CHANGE record, classifies the impact (CS/IG/SEC), and surfaces it for review. The pipeline handles the propagation — you do not need to manually update every artefact.

**If you are stuck:** Open the help chat. Ask "what do I do if X?" The training content is indexed and available.

---

## Before You Move to Your Role Module

Confirm you can answer these questions:

1. What is the difference between ground truth and an LLM inference?
2. What does GAP mean and what is your responsibility when the agent raises one?
3. Where do approved artefacts live after approval?
4. How do you resume a pipeline session after closing it?
5. What is a CHECK in a REQ file?

If you cannot answer all five, re-read the relevant section before proceeding.

---

## What's Next

Go to the module for your role:

| Role | Module |
|------|--------|
| BA / Product Owner | Module 1 — Requirements Discovery |
| Architect | Module 3 — Architecture |
| PxD / Designer | Module 2 — Prototype Builder, then Module 3b |
| Clinical Safety Officer | Module 4 — Clinical Safety |
| IG Owner / DPO | Module 5 — Information Governance |
| Security Reviewer | Module 6 — Security |
| Engineer | Module 7 — Engineering |

---

*Genesis AI Training — Module 0 v1.0 | July 2026*
*Next update: when GitHub integration (Plan 4c) and Context Graph (Plan KG) land*
