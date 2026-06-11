# SKILL: clin-wclin-registry-loader
# Phase: P06 Clinical Safety — Phase 0

## CLIN/WCLIN Registry Loader

**Purpose:** Load the authoritative list of relevant guardrails from `emis-x-api-clinical-safety` and `emis-x-webapp-clinical-safety` skill sets. These define the mandatory compliance domains that P06 must assess.

### Loading Protocol

At Phase 0 start, call `get_guardrail_details` for:
1. `emis-x-api-clinical-safety` — API-layer clinical safety rules (CLIN-001 through CLIN-006)
2. `emis-x-webapp-clinical-safety` — Frontend clinical safety rules (WCLIN-001 through WCLIN-006)

If `get_guardrail_details` returns content: log "✅ CLIN/WCLIN registry loaded. {N} rules active."
If unavailable: log "⚠️ CLIN/WCLIN registry unavailable. Proceeding with DCB0129 baseline only."

### Registry Summary (Baseline — from context)

**Key CLIN rules:**
- CLIN-001: Patient data handling — no PHI in logs
- CLIN-002: Clinical coding — SNOMED/Read code integrity
- CLIN-003: Prescriptions — audit trail mandatory
- CLIN-004: Allergy checks — must not be suppressible
- CLIN-005: Clinical decision support — evidence source must be traceable
- CLIN-006: Emergency access — must always be available

**Key WCLIN rules:**
- WCLIN-001: Patient context readiness before rendering clinical data
- WCLIN-002: Patient banner state handling
- WCLIN-003: Safe rendering of clinical content — no raw HTML injection
