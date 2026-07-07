# Genesis AI — Quick Reference Card
## Print this. Keep it next to you for your first week.

---

## The Pipeline at a Glance

| Stage | Who runs it | What it produces | Approver |
|-------|-------------|-----------------|----------|
| P01 | BA / PO | REQ-{id}.md — requirements, ACs, CHECKs, hazards | BA / PO |
| P02 | BA / PO / Designer | prototype/index.html — clickable EMIS-X prototype | BA / PO |
| P03 | Architect | ARCH-{id}.md — architecture, ADRs, data flows | Architect |
| P04 | Architect | API contracts, DB schema, Flyway migrations | Architect |
| P05 | PxD / Designer | PXD-{id}.md — component composition, interaction patterns | PxD Lead |
| P06 | CSO | DCB0129-{id}.md — hazard log, controls, sign-off | CSO |
| P07 | IG Owner / DPO | IG-{id}.md — DPIA, lawful basis, retention | IG Owner |
| P08 | Security Reviewer | SEC-{id}.md — threat model, ASVS controls | Security |
| P10 | All | Pre-swarm decision gate — consolidated review | All |
| P11 | Engineer + AI | Test suite + production code | Engineer |

---

## The Three Signals

When the agent raises one of these, stop and respond:

| Signal | Meaning | Your action |
|--------|---------|------------|
| **GAP** | Information is missing | Go and find it. Do not proceed without it. |
| **CLARIFICATION** | Ambiguous interpretation | Tell the agent which interpretation is correct. |
| **CONTRADICTION** | Two pieces of information conflict | Resolve the conflict before approving. |

---

## Ground Truth Checklist

Before every pipeline session, bring:
- [ ] The current EMIS Web behaviour (observed, not assumed)
- [ ] Known constraints from the EMIS-X architecture
- [ ] Any previous decisions from earlier pipeline stages
- [ ] Named stakeholders with named accountabilities
- [ ] Specific data: performance targets, volume estimates, retention periods

---

## What Makes a Good AC

```
Given [specific precondition]
When [specific action]
Then [specific, verifiable outcome]
```

Every AC must be testable by a machine without subjective judgement.

---

## What Makes a Good CHECK

```
CHECK N: {Descriptive name}
Setup: {Exact data state and system state}
Action: {Who does what}
Expected: {Exact observable outcome}
Pass criteria: {How to determine pass/fail objectively}
```

---

## When to Use `propose_requirement_change`

Use it when:
- A downstream stage discovers a gap in the REQ file
- An architectural constraint invalidates a REQ-level assumption
- A clinical safety assessment identifies a missing AC
- An engineering discovery reveals an unspecified behaviour

Never: edit an approved REQ file directly without a CHANGE record.

---

## Help Chat — What to Ask

| Question | When to ask it |
|----------|---------------|
| "What does P0{n} do?" | Before starting a new stage |
| "What is the status of this project?" | When inside a project |
| "What are the clinical safety guardrails for this increment?" | During engineering |
| "What did we decide about X in P01?" | When context is needed across stages |
| "What is the EMIS-X pattern for X?" | When prompting in P02 |
| "What CHECK covers X behaviour?" | During test writing |

---

## The 24-Hour Goal

After completing these modules, you should be able to:
- Open a real project in Genesis AI
- Run your role's pipeline stage end-to-end
- Approve the artefact with confidence
- Use the help chat to answer questions grounded in the approved specification

If you cannot do these things after 24 hours, open the help chat and ask "what am I stuck on in Module X?" — the training content is indexed and the agent will guide you.

---

## Version History

| Version | Date | What changed |
|---------|------|-------------|
| 1.0 | July 2026 | Initial release — P01-P08, P11 |
| 1.1 | TBD | Add Plan 4c — GitHub integration exercises |
| 1.2 | TBD | Add Plan KG — Context Graph exercises |
| 1.3 | TBD | Add Figma integration — P02 extension |
| 2.0 | TBD | Add Plan 5 — TDD Agent exercises |
| 2.1 | TBD | Add Plan 11 — Code Swarm exercises |

---

*Genesis AI Quick Reference v1.0 | July 2026*
