# SKILL: residual-risk-assessment
# Phase: P06 Clinical Safety — Phase 6

## Residual Risk Assessment

**Purpose:** Calculate residual risk after applying controls.

### Residual Risk Calculation

For each hazard with controls:
1. Reassess Likelihood after controls are applied (Severity should not change)
2. Calculate new risk score: Severity × Likelihood_residual
3. Determine if residual risk level is acceptable

### Acceptability Thresholds (EMIS Standard)

| Residual Level | Acceptable? |
|---------------|-------------|
| LOW (1–3) | ✅ ACCEPTABLE — document and proceed |
| MEDIUM (4–6) | ✅ ACCEPTABLE WITH MONITORING — document, add monitoring control |
| HIGH (8–12) | ⚠️ CONDITIONALLY ACCEPTABLE — requires CSO sign-off |
| CRITICAL (15–25) | ❌ NOT ACCEPTABLE — must return to Phase 5 and add stronger controls |

### If Residual Risk Remains CRITICAL

Do NOT write a CRITICAL residual risk to a REQ file. Instead:
1. Return to Phase 5 and design additional controls
2. If no feasible technical controls exist: escalate to CSO with design options
3. Add HIGH parking lot item: "CRITICAL residual risk on {HAZ-NNN} — CSO decision required"

### Residual Risk Template

```markdown
**Residual Risk Assessment:**
- Original risk: S{S} × L{L} = {Score} ({Level})
- Controls applied: {C-NNN, C-MMM}
- Residual likelihood: {L_residual}
- Residual risk: S{S} × L{L_residual} = {Residual_score} ({Residual_level})
- Acceptable: {Yes / Yes with monitoring / No — escalated}
```
