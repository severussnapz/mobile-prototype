# SKILL: ig003-gate-p07
# Phase: P07 Information Governance — Phase 0

## IG-003 Gate — Information Governance

**Purpose:** Determine whether a full or reduced IG assessment is required.

### Gate Questions

1. "Does this system process personal data (any identified or identifiable individual)?" → Yes = proceed. No = EXEMPT.
2. "Does this system process special category data (NHS Number, health data, biometrics)?" → Yes = ENHANCED (Article 9 rules apply).
3. "Does this system involve automated decision-making or profiling?" → Yes = Article 22 assessment required.
4. "Is this a minor change to an existing IG-assessed system?" → Yes = DELTA only.

### Gate Outcomes

| Outcome | Scope |
|---------|-------|
| ENHANCED | New system with special category data — full DPIA + Article 9 assessment |
| STANDARD | New system with personal data only — full DPIA |
| DELTA | Change to existing system — delta DPIA only |
| EXEMPT | No personal data processed — document basis for exemption |

Log: "IG-003 Gate: {outcome} — proceeding with {scope}."
