# SKILL: decision-log-p06
# Phase: P06 Clinical Safety — Phase 0

## Decision Log Protocol — P06

**Purpose:** Record all significant clinical safety decisions made during P06 for the formal DCB0129 evidence trail.

### Decisions to Log

- Hazard identification decisions ("Is X a hazard?" → decision + rationale)
- Risk rating decisions ("Severity 4 rather than 5 because...")
- Control decisions ("Control C-NNN accepted as sufficient because...")
- CSO overrides ("CSO changed severity from 3 to 4 because...")
- Scope decisions ("REQ-NNN has no clinical safety hazards because...")

### Decision Log File

Append to `feedback/P06_DECISION_LOG.md`:

```markdown
# P06 Decision Log

| Date | Decision | Rationale | Made by |
|------|---------|----------|---------|
| {DATE} | {Hazard HAZ-NNN: Severity = 4} | {Patient could receive incorrect dose if validation fails} | AI + CSO |
| {DATE} | {REQ-NNN: No clinical hazards} | {Reporting only, no write path to clinical data} | AI |
```
