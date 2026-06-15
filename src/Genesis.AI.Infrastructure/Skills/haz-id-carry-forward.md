# SKILL: haz-id-carry-forward
# Phase: P07 Information Governance — Phase 0

## Hazard ID Carry-Forward — P07

**Purpose:** Import clinical safety hazard references into the IG assessment for cross-domain risk tracking.

### Carry-Forward Protocol

1. Read all `## Clinical Safety (Added by Pipeline 06)` sections from `requirements/REQ-*.md`.
2. Extract all HIGH and CRITICAL hazards (residual risk ≥ 8).
3. For each, check: "Does this hazard have an IG dimension?" — e.g. unauthorised access to PHI, audit trail failure, data retention issue.
4. If yes: reference the HAZ-NNN ID in the P07 IG control section for that requirement.

### Cross-Reference Template

```markdown
### Clinical Safety Cross-Reference

| HAZ ID | IG Dimension | P07 Control |
|--------|------------|------------|
| HAZ-NNN | Unauthorised access to patient record | SEC-CTRL-NNN (access control policy) |
| HAZ-MMM | Audit trail failure | IG-CTRL-NNN (mandatory audit logging) |
```
