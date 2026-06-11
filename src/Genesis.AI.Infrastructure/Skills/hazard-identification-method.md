# SKILL: hazard-identification-method
# Phase: P06 Clinical Safety — Phase 1

## Hazard Identification Method

**Purpose:** Systematic method for identifying hazards in each requirement.

### Hazard Identification Questions (HAZOP-inspired)

For each requirement, ask ALL of the following:

1. **WRONG** — "Could the system deliver the wrong clinical information/action?"
2. **OMISSION** — "Could relevant clinical information be missing or not displayed?"
3. **DELAY** — "Could a delay in information delivery harm a patient?"
4. **UNAUTHORISED** — "Could an unauthorised user access or modify clinical data?"
5. **CORRUPTION** — "Could data be corrupted, lost, or shown in the wrong context?"
6. **OVERDOSE/UNDERDOSE** — "Could medication dosing be affected?"
7. **ALLERGY** — "Could allergy information be suppressed or missed?"
8. **AUDIT** — "Could clinical actions be performed without an audit trail?"
9. **CASCADING** — "Could a failure in this system cascade to another clinical system?"
10. **CONTEXT SWITCH** — "Could data from one patient be shown in another patient's context?"

### Hazard NOT Applicable

If a question does not apply to this requirement, log: "HAZ-HAZOP-{Q}: Not applicable — {reason}."

### Output per Hazard Found

```markdown
**{HAZ-NNN}** — {Hazard title}

**Trigger:** {What causes this hazard}
**Effect:** {What patient harm could result}
**HAZOP category:** {WRONG | OMISSION | DELAY | UNAUTHORISED | CORRUPTION | etc.}
**Affected requirements:** REQ-{NNN}
```
