# SKILL: dcb0129-compliance-check
# Phase: P06 Clinical Safety — Phase 10 (AUTO)

## DCB0129 Compliance Check

**Purpose:** Verify the clinical safety case meets DCB0129 mandatory requirements before CSO sign-off.

> 🤖 **AUTO PHASE:** Runs automatically after all hazard cards and guardrail mappings are complete.

### DCB0129 Mandatory Requirements Checklist

- [ ] Clinical Safety Officer is named and has appropriate competency
- [ ] All identified hazards have been assessed using the 5-point severity and likelihood scales
- [ ] All HIGH and CRITICAL hazards have at least one control
- [ ] All residual HIGH risks have CSO acknowledgement
- [ ] No CRITICAL residual risks exist (or all are escalated and CSO has documented decision)
- [ ] Hazard log is complete (no gaps in ID sequence from {HAZ-001} to {HAZ-NNN})
- [ ] All hazard cards are in IF678 format
- [ ] Guardrail mapping is complete

### Compliance Log

```
"DCB0129 compliance check:
- Hazards assessed: {N}
- HIGH risks with controls: {M}/{M_required}
- CRITICAL residual risks: {0 | N — escalated}
- Guardrail coverage: {N}/{N_total} hazards mapped
- All mandatory sections present: {Yes / No — missing: {list}}

DCB0129 compliance: {PASS | PARTIAL — issues: {list}}"
```
