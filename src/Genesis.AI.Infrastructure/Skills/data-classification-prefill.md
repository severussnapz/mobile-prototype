# SKILL: data-classification-prefill
# Phase: P07 Information Governance — Phase 2

## Data Classification — Pre-fill from Context

**Purpose:** Pre-populate data classification for each requirement using routing context and P06 outputs.

### Pre-fill Rules

From ROUTING CONTEXT `data_class`:
- `special_category` → Pre-fill all requirements as Special Category unless overridden
- `personal` → Pre-fill as Personal Data unless a requirement introduces special category data
- `anonymous` → Pre-fill as Anonymous — verify per requirement

### Data Classification Levels (EMIS Standard)

| Level | Description | Examples |
|-------|-------------|---------|
| Special Category | UK GDPR Article 9 data | NHS number, diagnosis, medication, allergy, biometric |
| Personal | Standard UK GDPR Article 6 | Name, DOB, address, email |
| Pseudonymous | Indirectly identifiable | Patient GUID (re-linkable) |
| Anonymous | Not identifiable | Aggregate statistics with k-anonymity ≥ 5 |

### Data Classification Template

```markdown
### Data Classification

| Data Type | Classification | Basis | Sensitivity |
|-----------|--------------|-------|------------|
| NHS Number | Special Category — Art 9 | Health/social care | RESTRICTED |
| Patient Name | Personal | Public task | INTERNAL |
| {Aggregate stat} | Anonymous | N/A | PUBLIC |
```
