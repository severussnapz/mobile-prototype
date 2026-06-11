# SKILL: cso-signoff-protocol
# Phase: P06 Clinical Safety — Phase 11

## CSO Sign-Off Protocol

**Purpose:** Obtain formal CSO review and sign-off for the clinical safety case.

### Sign-Off Steps

1. Present summary to CSO: "Clinical safety assessment complete for {PRODUCT_NAME}. Summary: {N} hazards identified, {M} HIGH, {K} MEDIUM, {J} LOW. All controls documented. DCB0129 compliance: {PASS/PARTIAL}."

2. Ask CSO to review:
   - "Please review the hazard log in `feedback/P06_REVIEW_LIST.md`"
   - "Any hazards you'd like to reassess?"
   - "Are all controls acceptable?"

3. Record CSO sign-off:

```markdown
## CSO Sign-Off

**Clinical Safety Officer:** {Name}
**Role:** {Role}
**Date:** {DATE}
**Decision:** {APPROVED | APPROVED WITH CONDITIONS | REJECTED}
**Conditions (if applicable):** {list}
**Signature:** Recorded by AI on behalf of {Name} — verbal/written confirmation obtained
```

4. If REJECTED: return to Phase 1 for the rejected hazards. Create HIGH parking lot items.

5. Update `feedback/P06_SESSION_LOG.md` with sign-off status.
