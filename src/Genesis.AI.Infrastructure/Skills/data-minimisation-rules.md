# SKILL: data-minimisation-rules
# Phase: P07 Information Governance — Phase 2

## Data Minimisation Rules

**Purpose:** Apply GDPR data minimisation principle to each requirement's data model.

### Data Minimisation Questions

For each data field designed in P04:
1. "Is this field necessary to fulfil the stated purpose?" → If No: remove it.
2. "Could a less privacy-invasive alternative achieve the same purpose?" → If Yes: use the alternative.
3. "Is this field shared with any third party?" → If Yes: apply data sharing agreement rules.
4. "Is this field retained longer than necessary?" → See `retention-deletion-prefill` skill.

### Prohibited Patterns

- **Storing data for potential future use** — data must be necessary for a current, defined purpose
- **Collecting more granular data than needed** — e.g. full address when postcode is sufficient
- **Logging PHI in application logs** — CLIN-001 / WSEC guardrail violation
- **Duplicating clinical records** locally — always reference the authoritative source

### Data Minimisation Output

```markdown
### Data Minimisation Assessment

| Field | Purpose | Necessary? | Alternative considered | Outcome |
|-------|---------|-----------|----------------------|---------|
| {FullName} | Display in UI | Yes | First name only | Keep — {reason} |
| {DOB} | Eligibility check | Yes | Age band | Keep — exact DOB required for clinical safety |
| {PostCode} | Service routing | Yes | LSOA code would suffice | Change to LSOA |
```
