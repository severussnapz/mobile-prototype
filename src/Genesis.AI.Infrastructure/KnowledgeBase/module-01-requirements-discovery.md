# Genesis AI — Module 1: Requirements Discovery
## Role: BA / Product Owner | Est. 45 minutes
### Prerequisite: Module 0 complete

---

## What This Module Covers

P01 is the foundation of everything. Every downstream stage — prototype, architecture, clinical safety, IG, security, and ultimately the code — derives from the REQ file you produce here. If the REQ file is weak, everything downstream is weak.

Your job in P01 is not to fill in a form. It is to conduct a structured requirements interview — with yourself, your stakeholders, and the agent acting as a disciplined interviewer.

---

## What P01 Produces

A single markdown file: `requirements/REQ-{id}-{feature-name}.md`

This file contains:
- **Business Context** — why this feature exists, what problem it solves, which stakeholders are affected
- **User Personas** — who uses this feature, in what context, with what goals
- **Requirements** — each requirement with a minimum of 3 Acceptance Criteria
- **CHECKs** — named, executable test scenarios derived from the ACs
- **Hazard Log** — HAZ-IDs for any requirement that touches patient safety
- **ADRs** — Architectural Decision Records for decisions made at requirements stage
- **DB Schema** — proposed tables and columns (high level)
- **Component Interfaces** — proposed API contracts (high level)
- **OTEL Spans** — observability requirements

Every requirement must have a minimum of 3 ACs. Every AC that touches clinical safety must have a HAZ-ID. Every HAZ-ID flows into P06. The CHECKs flow directly into the TDD agent in P11.

---

## The P01 Interview Engine

P01 runs in phases. You cannot skip a phase. The agent will not advance until each phase is complete.

| Phase | Name | What you provide |
|-------|------|-----------------|
| 0 | Business Context | Why this feature exists, stakeholder impact, release type |
| 1 | User Personas | Who uses it, in what context |
| 2 | Non-Functional Requirements | Performance, scalability, reliability targets |
| 3 | Clinical Safety anchor | Does this touch patient data? Any known hazards? |
| 4 | IG anchor | What personal data is processed? Any third-party sharing? |
| 5 | Security anchor | Authentication, authorisation, data sensitivity |

Phases 3, 4, and 5 are **lightweight anchors only** — the deep elicitation happens in P06, P07, and P08 respectively. Your job here is to flag the concerns so the downstream stage knows where to focus.

---

## What Makes a Good Acceptance Criterion

An AC must be:
- **Testable** — a machine or human can determine pass/fail without subjective judgement
- **Specific** — names the exact behaviour, not a vague outcome
- **Complete** — covers the happy path AND the error path

### Format: Given / When / Then

```
Given [a specific precondition]
When [a specific action is taken]
Then [a specific, verifiable outcome]
```

### Example — Good AC
```
Given a GP has an appointment booked for a patient with a valid mobile number
When the reminder job runs 48 hours before the appointment
Then the patient receives an SMS containing the appointment date, time,
     practice name, and an opt-out link — within 60 seconds of the job running
```

### Example — Bad AC
```
The system should send reminders to patients before appointments.
```

The bad AC is not testable. "Should send" by when? To which patients? What constitutes a reminder? When does it run? What does "before" mean?

---

## What Makes a Good CHECK

A CHECK is a named, executable test scenario. The TDD agent in P11 reads CHECKs and generates test code from them. If a CHECK is vague, the generated test will be vague.

### Format

```
CHECK N: {Descriptive name}
Setup: {Exact preconditions — data state, system state, user state}
Action: {What happens — who does what}
Expected: {Exact observable outcome}
Pass criteria: {How to determine pass/fail objectively}
```

### Example — Good CHECK
```
CHECK 3: Reminder not sent when patient has opted out
Setup: Patient P has opted out of appointment reminders (opt_out = true in DB).
       GP has appointment A booked for patient P in 48 hours.
Action: Reminder job runs.
Expected: No SMS or email is sent to patient P for appointment A.
          An audit record is created: { patient_id: P, appointment_id: A,
          reason: "opted_out", sent: false }
Pass criteria: SMS provider receives zero API calls for patient P.
               audit_log contains one record matching the above.
```

---

## Exercise 1: Start a P01 Session

1. Open Genesis AI → Projects → "GP Appointment Reminders (Training)"
2. Click on the P01 stage — Requirements Discovery
3. Start a new conversation
4. The agent will ask its Phase 0 questions. Answer them using the context below:

**Business Context to provide:**
> "We are building a GP appointment reminder notification feature for EMIS-X. The feature sends automated SMS and email reminders to patients 48 hours before a scheduled GP appointment. The goal is to reduce DNA (Did Not Attend) rates, which currently run at approximately 8% across practices using EMIS Web. The feature is part of the EMIS-X GP Products increment. The release type is EMIS-X. Assurance is required. The practice manager and the GP are the primary stakeholders. The patient is the recipient."

5. Continue through each phase — answer the questions the agent asks
6. When the agent proposes an AC, challenge at least one of them: ask "why does this need 3 separate checks rather than one?"

**What to notice:** The agent pushes back when you try to skip information. If you give a vague answer, it will ask the question again with more specificity. This is intentional.

---

## Exercise 2: Handle a GAP Response

During your P01 session, the agent will raise a GAP at some point — typically around the opt-out mechanism or the SMS provider contract.

When it does:
1. Do not deflect ("we'll figure that out later")
2. Do not invent ("I think it works like...")
3. Either: provide the actual information
4. Or: acknowledge the gap explicitly — "we do not yet know the SMS provider — this is an open question. I am flagging it in the parking lot."

The agent will add it to the parking lot. You can return to it in the next session.

**Key learning:** A GAP in P01 that is not resolved before P06 will cause the clinical safety agent to make assumptions. Those assumptions may be wrong. Resolve gaps early.

---

## Exercise 3: Approve Your First REQ File

Once you have completed all mandatory phases:
1. The agent will present a draft REQ file
2. Read it end to end — do not click Approve immediately
3. Check: does every requirement have at least 3 ACs?
4. Check: are there any CHECKs that are not objectively testable?
5. Check: has the agent identified the opt-out requirement as a clinical safety concern?

If the answer to any of these is no, work with the agent to correct it before approving.

When you are satisfied: click Approve.

**What happens on approval:**
- The REQ file is stored in S3
- It is indexed into the project knowledge base
- P02 (Prototype) is unblocked
- The help chat can now answer questions about this requirement

---

## How to Handle Requirement Changes

After approval, something will change. A stakeholder will add a new constraint. A technical discovery in P03 will invalidate an assumption. A clinical safety concern in P06 will require an AC to be strengthened.

**Do not edit the approved REQ file directly without using `propose_requirement_change`.**

The change management flow:
1. In any pipeline conversation, type the proposed change
2. The agent classifies it: low/medium/high impact, CS/IG/SEC flags
3. A CHANGE record is created
4. You review and approve or reject the change
5. On approval, the REQ file is amended and the CHANGE record is committed

This is not bureaucracy. This is what gives you a complete, auditable trail of every decision made during the requirements phase. When a regulator asks "why was this AC added?" the CHANGE record has the answer.

---

## Common Mistakes in P01

**Mistake 1: Accepting the first AC the agent proposes**
The agent's first AC is a starting point. Challenge it. Ask "what happens when the patient has no mobile number?" Ask "what happens when the SMS provider is unavailable?" The agent will strengthen the AC.

**Mistake 2: Skipping the NFR phase**
"It should be fast" is not a non-functional requirement. "The reminder job must complete processing of all appointments in the 48-hour window within 5 minutes, with P95 SMS delivery within 60 seconds of dispatch" is a non-functional requirement. The TDD agent cannot generate a performance test from "it should be fast."

**Mistake 3: Not flagging clinical concerns in Phase 3**
If you skip the clinical safety anchor in Phase 3, the P06 agent starts with no prior context. It will ask the same questions you should have answered in P01. You will have duplicated effort and weakened the traceability chain.

**Mistake 4: Using the parking lot as a skip mechanism**
The parking lot exists for genuinely deferred items — decisions that require information you cannot get in this session. It is not for items you do not want to think about. If something ends up in the parking lot three sessions in a row, it is a gap that is blocking downstream work.

---

## Extension: When GitHub Integration Lands (Plan 4c)

When Plan 4c is live, every approved REQ file will be committed by `genesis-ai[bot]` to:
```
{feature-repo}/.genesis/requirements/REQ-{id}-{feature-name}.md
```

Every amendment will be a Git diff. Every version will be recoverable. The CHANGE records will be committed alongside. This gives you an immutable, auditable requirements trail in the same repository as the code — no separate system, no manual sync.

---

## Checklist Before Moving to P02

- [ ] All mandatory phases complete (BC, UP, NFR, CS anchor, IG anchor, SEC anchor)
- [ ] Every requirement has at least 3 ACs
- [ ] Every AC is objectively testable
- [ ] Every AC touching patient data has a HAZ-ID
- [ ] All CHECKs are named and executable
- [ ] The parking lot contains only genuinely deferred items with owners
- [ ] REQ file approved

When all boxes are checked: proceed to Module 2 (Prototype Builder).

---

*Genesis AI Training — Module 1 v1.0 | July 2026*
*Next update: when Plan 4c (GitHub integration) and Plan KG (Context Graph) land*
