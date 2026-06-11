# SKILL: plain-language-rule
# Phase: P06 Clinical Safety — Phase 1

## Plain Language Rule — Hazard Descriptions

**Purpose:** Ensure hazard descriptions are written in clear, non-technical language suitable for CSO review and regulatory submission.

### Rules

**DO write:**
- "A clinician could prescribe a medication the patient is allergic to because the allergy warning is not displayed on the prescription screen."
- "A patient could receive a duplicate dose because the system does not check for recently dispensed medications."

**DO NOT write:**
- "NullReferenceException in AllergyService leads to missing allergy alert."
- "FK constraint violation could cause data inconsistency."

### Plain Language Test

Before writing a hazard description, ask: "Could a non-technical clinician understand this hazard from this description?" If No: rewrite.

### Hazard Description Template

```
{Patient/clinician/pharmacist} could {harmful outcome} because {system condition/failure}.
```

Example: "A clinician could administer the wrong medication dose because the weight-based dose calculator uses kilograms but the patient weight field accepts pounds without conversion."
