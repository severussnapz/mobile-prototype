---
name: human-in-the-loop-protocol
description: 'Use this skill in P06, P07, P08 for all phases involving CSO, DPO, or security reviewer decisions. Defines the never-autonomous list, decision authority rules, and the hard stop protocol when a mandatory human decision is absent.'
metadata:
  version: 1.0.0
  applyTo:
    - requirements
---

# Human-in-the-Loop Protocol

## Core Principle

The AI never makes a clinical safety, information governance, or security decision autonomously.
The AI presents, derives, calculates, and assembles. The human confirms, rejects, or modifies.

## Decision Authority Map

| Decision type | Authority | AI role |
|---------------|-----------|---------|
| Hazard identification | CSO confirms/modifies | AI pre-fills from Dimension 1 notes (rich) or elicits (thin) |
| Severity assignment | CSO assigns | AI presents the 5-point scale; CSO picks the number |
| Likelihood assignment | CSO assigns | AI presents the scale; CSO picks the value |
| Control acceptance | CSO accepts/rejects each | AI pre-fills from CLIN skill; CSO decides per control |
| ALARP decision | CSO decides | AI calculates residual risk; CSO confirms ALARP |
| CSO sign-off | CSO | AI prepares the sign-off artefact; CSO must confirm |
| Lawful basis | DPO/IG lead confirms | AI pre-fills candidate basis; DPO decides |
| Data classification | DPO/IG lead confirms | AI derives from P04 schema; DPO confirms or adjusts |
| Retention schedule | DPO/IG lead confirms | AI pre-fills from NHS Code of Practice; DPO confirms or adds exception |
| IG reviewer pass | Named reviewer | Must be a different person from the producer |
| Threat framing | Security lead confirms | AI uses P03 framing as starting point; security lead confirms assets, actors, entry points |
| Control strategy | Security lead accepts/rejects | AI pre-fills standard controls; security lead decides |
| Security reviewer pass | Named reviewer | Must be a different person from the producer |

## Never-Autonomous List

The following decisions MUST NEVER be made by the AI without explicit human confirmation.
If reached without a human response, emit a hard stop.

- Hazard severity score
- Hazard likelihood score  
- ALARP determination
- Lawful basis selection
- Retention period when not covered by NHS Code of Practice standard schedule
- Any decision that would lower a risk rating from a prior iteration
- Sign-off on behalf of a named CSO, DPO, or reviewer

## Hard Stop Protocol

When a mandatory human decision has not been provided:

```
🛑 HARD STOP — Human decision required

Decision type: [type from Decision Authority Map]
Authority: [CSO / DPO / Security Lead / Named Reviewer]
Context: [what is being decided and why it cannot be assumed]

I cannot proceed until this decision is provided.
Please [specific action required].
```

Do not advance the phase. Do not produce artefacts. Do not add to the parking lot.
The conversation must pause here until the human provides the decision.

## Documenting Human Decisions

Every confirmed human decision must be recorded in the stage `DECISION_LOG`:

```markdown
## Decision [N] — [date]

**Decision type:** [type]
**Authority:** [name or role]
**Context:** [what was being decided]
**Decision:** [verbatim or paraphrased decision]
**Recorded by:** AI agent from conversation turn [N]
```
