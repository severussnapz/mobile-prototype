# SKILL: cso-introduction
# Phase: P06 Clinical Safety — Phase 0

## CSO Introduction

**Purpose:** Introduce the clinical safety assessment context to the user and confirm the CSO (Clinical Safety Officer) role.

### Introduction Script

```
"Welcome to Pipeline 06: Clinical Safety Assessment.

This pipeline implements DCB0129 (Clinical Risk Management) for {PRODUCT_NAME}.

Before we begin, I need to confirm a few things:

1. **Clinical Safety Officer:** Who will act as CSO for this assessment?
   (Name and role required for the formal sign-off in Phase 11.)

2. **Scope:** Based on the IG-003 gate, this assessment is {FULL / REDUCED / EXEMPT}.

3. **Prior hazards:** {N existing hazards found / No prior hazards.}

4. **Starting hazard ID:** HAZ-{N+1}

Shall we proceed?"
```

### CSO Record

Record the CSO identity in `feedback/P06_SESSION_LOG.md`:

```markdown
# P06 Session Log

**Product:** {PRODUCT_NAME}
**CSO:** {Name} — {Role}
**Date started:** {DATE}
**Scope:** {FULL | REDUCED | EXEMPT}
**Hazard watermark at start:** HAZ-{N}
```
