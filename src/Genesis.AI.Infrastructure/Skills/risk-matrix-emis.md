# SKILL: risk-matrix-emis
# Phase: P06 Clinical Safety — Phase 4 (AUTO)

## Risk Matrix — EMIS Standard

**Purpose:** Auto-calculate risk rating from Severity × Likelihood using the EMIS standard matrix.

> 🤖 **AUTO PHASE:** This phase runs automatically when both Severity (Phase 2) and Likelihood (Phase 3) are confirmed. No user input required.

### Risk Matrix (Severity × Likelihood)

|           | S=1 | S=2 | S=3 | S=4 | S=5 |
|-----------|-----|-----|-----|-----|-----|
| **L=5**   | 5   | 10  | 15  | 20  | 25  |
| **L=4**   | 4   | 8   | 12  | 16  | 20  |
| **L=3**   | 3   | 6   | 9   | 12  | 15  |
| **L=2**   | 2   | 4   | 6   | 8   | 10  |
| **L=1**   | 1   | 2   | 3   | 4   | 5   |

### Risk Levels

| Score | Level | Action Required |
|-------|-------|----------------|
| 1–3 | LOW | Document. No immediate action. Review at next release. |
| 4–6 | MEDIUM | Document. Implement controls before go-live. |
| 8–12 | HIGH | BLOCK. Controls must be designed and confirmed before proceeding. |
| 15–25 | CRITICAL | BLOCK. Escalate to CSO. Controls must be approved before any further development. |

### Auto-Log

```
"Auto-calculated risk for {HAZ-NNN}: Severity {S} × Likelihood {L} = {Score} → {Level}."
```
