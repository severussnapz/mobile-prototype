# SKILL: privacy-by-design-checklist
# Phase: P07 Information Governance — Phase 4.5 (AUTO)

## Privacy by Design Checklist

**Purpose:** Auto-verify that P04 design decisions satisfy privacy by design principles.

> 🤖 **AUTO PHASE:** Runs after IG control mapping is complete for each requirement.

### Privacy by Design Principles (ICO)

| Principle | Check | Pass condition |
|---------|-------|---------------|
| Proactive, not reactive | Hazards addressed before go-live | All HIGH+ hazards have controls |
| Privacy as default | Minimum necessary data is the default | Data minimisation assessment passed |
| Privacy embedded | Privacy not an add-on | IG controls designed in P04, not added post-hoc |
| Full functionality | Privacy AND function | No privacy control that breaks the clinical function |
| End-to-end security | Lifecycle protection | Encryption at rest and in transit |
| Visibility | Transparent data practices | Data processing described in DPIA |
| Respect for users | User rights respected | Subject access request tooling exists |

### Auto-Check Output

```
"Privacy by Design auto-check for REQ-{NNN}:
- Proactive: {Pass / Fail — {reason}}
- Privacy as default: {Pass / Fail — {data minimisation issue}}
- End-to-end security: {Pass / Fail — {encryption gap}}

Overall: {PASS / FAIL — remediation required}"
```

If any FAIL: create a MEDIUM parking lot item for the remediation.
