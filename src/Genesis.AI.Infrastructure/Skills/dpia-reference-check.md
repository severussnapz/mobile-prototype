# SKILL: dpia-reference-check
# Phase: P07 Information Governance — Phase 0

## DPIA Reference Check

**Purpose:** Determine whether an existing DPIA covers this product and what delta assessment is required.

### Check Protocol

1. Search for `dpia_reference` in Project Foundation / manifest.
2. If found: load the DPIA reference document. Extract:
   - Lawful basis
   - Special category grounds (Article 9)
   - Data subjects listed
   - Data flows documented
   - Retention periods
3. Compare with the current product requirements. Identify gaps:
   - New personal data types not in existing DPIA
   - New data flows not documented
   - New purposes not covered by existing lawful basis

### Delta Assessment Output

```markdown
### DPIA Delta Analysis

**Existing DPIA:** {Reference}
**New personal data types not covered:** {list or "None"}
**New data flows not covered:** {list or "None"}
**New purposes not covered:** {list or "None"}
**Delta assessment required:** {Yes — covering: {list} / No}
```
