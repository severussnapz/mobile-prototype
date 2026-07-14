# Genesis AI — Modules 4–7: Reviewers and Engineering
## Roles: CSO | IG Owner | Security Reviewer | Engineer
### Prerequisite: Module 0 complete

---

# Module 4: Clinical Safety (P06)
## Role: Clinical Safety Officer | Est. 30 minutes

---

## What P06 Produces

`clinical-safety/DCB0129-{id}.md` — a DCB0129 Clinical Safety Case for the increment.

This is a formal regulatory document. It must be complete, traceable, and signed off by the named CSO before the feature goes to production. It contains:
- Every hazard identified (HAZ-IDs traced back to the REQ file)
- Severity and likelihood scoring per DCB0129
- Control measures and residual risk
- Guardrails — CHECKs that validate clinical safety in the deployed code
- Your sign-off record

On approval, a companion `DCB0129-{id}.xlsx` is generated matching the CS team's existing Excel schema, and the hazard tracking DB is updated via API.

> **Coming in Plan 4c:** The approved DCB0129 artefact will also be committed to `.genesis/clinical-safety/` in the feature repo by `genesis-ai[bot]`.

---

## What the Agent Has Already Done

When you open P06, the agent has already:
- Read every HAZ-ID in the REQ file
- Populated an initial hazard log with the identified hazards
- Pre-scored severity and likelihood based on DCB0129 category tables
- Proposed control measures aligned with existing EMIS-X mitigations
- Populated the guardrails section with CHECKs from the REQ file

Your job is not to start from scratch. Your job is to **review, challenge, and strengthen** what the agent has produced.

---

## Exercise 1: Review the Pre-Populated Hazard Log

1. Open Genesis AI → Projects → "GP Appointment Reminders (Training)"
2. Open the P06 stage
3. Read the pre-populated `DCB0129-001-appointment-reminders.md`
4. Identify the three highest-severity hazards in the log

**Question to answer:** The agent has classified "patient receives reminder for a cancelled appointment" as medium severity. Do you agree? What is the worst-case clinical outcome if a patient attends a cancelled appointment and the practice is not expecting them?

---

## Exercise 2: Challenge a Mitigation

1. Find the hazard: "Reminder sent to wrong patient due to incorrect mobile number mapping"
2. The agent has proposed the mitigation: "Validate mobile number against NHS Spine PDS before dispatch"
3. Challenge it: ask the agent "what happens if PDS validation fails transiently — does the reminder get suppressed entirely or retried?"
4. If the answer does not satisfy you, ask it to strengthen the control measure

**Key learning:** A mitigation that does not account for its own failure mode is not a complete mitigation. Every control measure must have a failure behaviour.

---

## Exercise 3: Add a Hazard the Agent Missed

The agent has not identified this hazard: a patient who has opted out of electronic communications (under a reasonable adjustment request) still receives a reminder because the opt-out flag is stored in a separate system that the notification service does not query.

1. In the P06 conversation, raise this hazard
2. Work with the agent to score it, propose a control measure, and add it to the hazard log
3. The agent will propose raising a CHANGE record to add the relevant AC to the REQ file

**What to notice:** Adding a new hazard in P06 that traces back to a gap in P01 is the correct process. The CHANGE record propagates the fix back to the REQ file. The audit trail is complete.

---

## Your Sign-Off Accountability

When you approve the DCB0129 artefact, you are signing off that:
- Every identified hazard has been assessed with appropriate severity and likelihood
- Every control measure is adequate and its failure behaviour is defined
- The residual risk is ALARP (As Low As Reasonably Practicable)
- The guardrails in the code will enforce the clinical safety constraints

This is a regulatory accountability. It is not a formality. If a patient is harmed because a hazard was not identified or a mitigation was inadequate, the signed DCB0129 is the evidence trail.

---

---

# Module 5: Information Governance (P07)
## Role: IG Owner / DPO | Est. 30 minutes

---

## What P07 Produces

`ig/IG-{id}.md` — a Data Protection Impact Assessment (DPIA) and IG compliance record for the increment.

Contains:
- All personal data flows (what is processed, where it goes, how long it is retained)
- Lawful basis for processing (UK GDPR Article 9 for special category health data)
- Data minimisation assessment
- Retention and deletion policy
- Third-party data sharing assessment
- Your DPIA conclusion

---

## What the Agent Has Already Done

The agent has read the REQ file and the architecture artefact. It has:
- Identified all personal data elements mentioned across both documents
- Proposed a lawful basis for each data flow
- Mapped retention periods against EMIS-standard data classifications
- Flagged any third-party data sharing (e.g. SMS provider receiving patient mobile numbers)

---

## Exercise 1: Identify All Personal Data Flows

1. Open the pre-populated IG artefact for the test project
2. List every personal data element:
   - Patient mobile number (sent to SMS provider)
   - Patient email address (sent to email provider)
   - Appointment date/time (linked to patient record)
   - Opt-out status (patient preference data)
3. For each: confirm the proposed lawful basis is correct

**Question:** The agent has proposed "legitimate interests" as the lawful basis for sending appointment reminders. UK GDPR Article 9 applies to health data. Is "legitimate interests" a valid lawful basis under Article 9? If not, what is the correct basis?

---

## Exercise 2: Challenge the Third-Party Sharing Assessment

1. The SMS provider receives patient mobile numbers
2. The agent has proposed a Data Processing Agreement (DPA) as the control measure
3. Ask the agent: "What is the SMS provider's data residency? Are patient mobile numbers leaving the UK?"
4. If the answer is uncertain, raise it as a GAP — this must be resolved before approval

---

## Exercise 3: Retention and Deletion

1. The agent has proposed a 12-month retention period for notification audit logs
2. Challenge it: "Our standard retention for clinical audit logs is 7 years. Why has 12 months been proposed here?"
3. Work with the agent to align the retention period with the EMIS data classification standard

---

---

# Module 6: Security (P08)
## Role: Security Reviewer | Est. 30 minutes

---

## What P08 Produces

`security/SEC-{id}.md` — a security review covering OWASP ASVS controls, threat modelling, and security architecture decisions.

Contains:
- STRIDE threat model for the increment
- OWASP ASVS control mapping (Level 2 minimum for EMIS-X)
- Attack vector coverage
- Security ADRs
- Penetration test scope recommendation
- Your sign-off

---

## What the Agent Has Already Done

The agent has read the REQ file, architecture, and IG artefact. It has:
- Conducted an initial STRIDE threat model against the architecture
- Mapped the proposed API contracts to OWASP ASVS Level 2 controls
- Identified authentication and authorisation requirements from the REQ file
- Flagged any third-party integrations as attack surface

---

## Exercise 1: Review the OWASP ASVS Mapping

1. Open the pre-populated SEC artefact for the test project
2. Find the ASVS mapping for the notification dispatch endpoint
3. Confirm that V2 (Authentication), V4 (Access Control), and V7 (Error Handling) are all mapped
4. Identify any control that has been marked "N/A" — challenge every N/A

**Key principle:** In EMIS-X, nothing that touches patient data should have an unanswered ASVS control. "N/A" requires explicit justification.

---

## Exercise 2: Add a STRIDE Threat

The agent has not identified this threat: an external attacker enumerates patient appointment times by sending crafted opt-out requests with sequential patient identifiers and observing the response time difference.

1. Raise this threat in the P08 conversation
2. Work with the agent to classify it (Information Disclosure in STRIDE)
3. Propose a control: rate limiting on the opt-out endpoint, opaque patient identifiers in opt-out links
4. Add the control to the security review

---

## Your Sign-Off Accountability

When you approve the SEC artefact, you are confirming that:
- The STRIDE model covers the realistic attack surface
- The ASVS controls are mapped and justified
- Any gaps have been raised as CHANGE records and resolved
- The penetration test scope recommendation is appropriate

---

---

# Module 7: Engineering (P11)
## Role: Engineer | Est. 30 minutes
### Prerequisite: Module 0 complete. Read the approved REQ, ARCH, and DCB0129 artefacts for your increment before starting.

---

## What You Receive from Genesis AI

By the time you receive an increment, the following are complete and approved:
- `REQ-{id}.md` — full requirements, ACs, CHECKs, hazard log, DB schema, API contracts
- `ARCH-{id}.md` — service decomposition, ADRs, data flows, OpenAPI contracts, Flyway migrations, EF entity configurations
- `DCB0129-{id}.md` — hazard log, control measures, guardrails
- `IG-{id}.md` — data flows, retention, lawful basis
- `SEC-{id}.md` — threat model, ASVS controls
- `prototype/index.html` — the approved clickable prototype

You do not write requirements. You do not make architectural decisions. You implement what has been specified, and you raise a CHANGE record if the specification is incomplete or incorrect.

---

## How to Read a REQ File as an Engineer

The sections you care most about:

**CHECKs** — these become your acceptance tests. Each CHECK is a named, executable scenario. The TDD agent in P11 reads these and generates test code. Your job is to make those tests pass.

**DB Schema** — the proposed tables and columns. The ARCH artefact has the Flyway migration SQL. Do not modify it — raise a CHANGE record if it is wrong.

**Component Interfaces** — the proposed API contracts. The ARCH artefact has the OpenAPI specification. Implement exactly what is specified.

**Guardrails** — the clinical safety controls that must be enforced in code. These are not optional. They are regulatory requirements with a CSO's sign-off behind them.

---

## Exercise 1: Map CHECKs to Test Scenarios

1. Open the REQ file for the test project
2. Find all CHECKs in the document
3. For each CHECK, write one sentence describing what the test will assert

**Example:**
```
CHECK 3: Reminder not sent when patient has opted out
Test assertion: When the reminder job runs, zero SMS/email API calls are made
for any patient with opt_out = true. The audit log records one entry per
opted-out patient with sent = false and reason = "opted_out".
```

**What to notice:** A well-written CHECK gives you the test assertion directly. You should not need to infer what to test from the business requirement — the CHECK specifies it.

---

## Exercise 2: Use the Help Chat to Query the Project

1. Click `?` to open the help chat
2. Ask: "What are the clinical safety guardrails for the appointment reminder increment?"
3. Ask: "What is the data retention policy for notification audit logs?"
4. Ask: "What does CHECK 5 require?"

**What to notice:** The help chat has access to all approved artefacts for this project. You do not need to search through documents — ask the question and get the answer grounded in the approved specification.

---

## Exercise 3: Raise a Question Back to the Pipeline

During implementation, you discover that the Flyway migration in ARCH-001 does not include an index on `notification_record.appointment_id`, but the query plan shows a sequential scan on this column for every reminder job run.

1. In the help chat, ask: "Is there an index on notification_record.appointment_id?"
2. The help chat will confirm: no index is specified in the ARCH artefact
3. Raise a CHANGE record: `propose_requirement_change` — add a non-functional requirement for the index and an ADR explaining why it is needed
4. The CHANGE is reviewed and approved — the migration is updated

**Key learning:** Never silently add behaviour that is not in the specification. The CHANGE record creates the audit trail. If the index is added without a CHANGE record, there is no documented rationale — and no traceability to a business or clinical requirement.

---

## What You Should Never Have to Ask Twice

If the specification is complete, you should never need to ask:
- "What does this endpoint return?" — it is in the OpenAPI contract in ARCH
- "What database tables do I need?" — they are in the Flyway migration in ARCH
- "What are the clinical safety constraints?" — they are in the guardrails section of DCB0129
- "What does the UI look like?" — it is in the approved prototype
- "What is the retention policy for this data?" — it is in IG

If you are asking any of these questions, either the specification is incomplete (raise a CHANGE record) or you have not read the artefacts (read them).

---

## Extension: When Plan 5 (TDD Agent) and Plan 11 (Code Swarm) Land

When the TDD agent is live:
- Agent A reads the REQ file CHECKs and generates a failing test suite
- Agent B reads the ARCH artefact and writes code to make the tests pass
- The Review Agent checks every output against the clinical safety guardrails and EMIS-X coding standards

Your role shifts from writing implementation code to reviewing AI-generated code against the specification. The specification is still the source of truth. The tests are still the quality gate. The guardrails are still regulatory requirements.

When the Context Graph is live:
- The code swarm has access to every previously approved implementation pattern across all EMIS-X repos
- Blast radius analysis tells you which existing tests are at risk from a change before you make it
- Migration status tells you which EMIS Web capabilities have already been migrated and what patterns were used

---

## Checklist Before Closing an Increment

- [ ] All CHECKs from the REQ file have corresponding passing tests
- [ ] All clinical safety guardrails are enforced in code and tested
- [ ] All OWASP ASVS controls flagged in SEC are implemented
- [ ] All Flyway migrations match the ARCH artefact exactly
- [ ] OTEL spans required by the REQ file are instrumented
- [ ] Any gaps discovered during implementation have CHANGE records
- [ ] All CHANGE records are approved before the PR is raised

---

*Genesis AI Training — Modules 4–7 v1.0 | July 2026*
*Next update: when Plan 4c (GitHub integration), Plan 5 (TDD Agent), Plan KG (Context Graph), and Plan 11 (Code Swarm) land*
