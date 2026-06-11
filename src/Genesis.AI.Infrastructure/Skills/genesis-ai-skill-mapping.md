# SKILL: genesis-ai-skill-mapping
# Phase: P06 Clinical Safety — Phase 9 (AUTO)

## Genesis AI Skill Mapping — Clinical Safety

**Purpose:** Map identified hazards to the Genesis AI CLIN/WCLIN guardrail rules that address them.

> 🤖 **AUTO PHASE:** After all hazard cards are complete, auto-generate the skill mapping table.

### Mapping Protocol

For each hazard, identify which CLIN/WCLIN guardrail(s) are the relevant controls:

| Hazard category | Relevant guardrails |
|----------------|---------------------|
| PHI in logs | CLIN-001 |
| SNOMED/Read code corruption | CLIN-002 |
| Prescription audit trail missing | CLIN-003 |
| Allergy suppression | CLIN-004 |
| Clinical decision support | CLIN-005 |
| Emergency access | CLIN-006 |
| Patient context timing | WCLIN-001 |
| Patient banner state | WCLIN-002 |
| Raw HTML in clinical display | WCLIN-003 |

### Output

```markdown
## Guardrail Mapping

| Hazard | Guardrail | Coverage |
|--------|---------|---------|
| HAZ-NNN | CLIN-NNN | {Full / Partial} |
| HAZ-NNN | WCLIN-NNN | {Full / Partial} |

**Unmapped hazards (no guardrail coverage):**
| HAZ-NNN | {Hazard title} | Manual control required — no existing guardrail |
```

Unmapped hazards MUST have a manual control designed in Phase 5.
