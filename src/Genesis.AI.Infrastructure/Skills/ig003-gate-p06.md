# SKILL: ig003-gate-p06
# Phase: P06 Clinical Safety — Phase 0

## IG-003 Gate — Clinical Safety

**Purpose:** Check whether this product requires a full DCB0129 clinical safety case or a reduced-scope assessment.

### Gate Questions

1. "Does this system handle patient-identifiable clinical data?" → Yes = FULL DCB0129. No = skip.
2. "Does this system support clinical decision-making?" → Yes = FULL DCB0129.
3. "Is this a minor change to an existing certified system?" → Yes = REDUCED scope (delta assessment only).
4. "Is the compliance domain 'generic' or 'finance'?" → REDUCED scope.

### Gate Outcomes

| Outcome | Description | P06 Scope |
|---------|-------------|-----------|
| FULL | New clinical system or major change | All 13 phases |
| REDUCED | Minor change / delta | Phases 0, 1, 7 (risk delta), 11, 12, 13 |
| EXEMPT | Non-clinical, no patient data | Skip all phases — write IG-003 EXEMPT declaration |

### EXEMPT Declaration Template

```markdown
## Clinical Safety (Added by Pipeline 06)

**IG-003 Gate Result: EXEMPT**

This system does not handle patient-identifiable clinical data and does not support
clinical decision-making. DCB0129 clinical risk management does not apply.

**Declared by:** AI assistant on behalf of {PRODUCT_NAME} design team
**Date:** {DATE}
```

Log gate result: "IG-003 Gate: {FULL|REDUCED|EXEMPT} — proceeding with {scope}."
