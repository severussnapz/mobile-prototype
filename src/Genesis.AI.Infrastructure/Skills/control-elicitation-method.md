# SKILL: control-elicitation-method
# Phase: P06 Clinical Safety — Phase 5

## Control Elicitation Method

**Purpose:** Systematically identify controls to reduce risk for each hazard.

### Control Categories

| Category | Description | Examples |
|---------|-------------|---------|
| PREVENT | Prevents the hazard from occurring | Validation check, allergy alert, access control |
| DETECT | Detects the hazard has occurred | Audit log, monitoring alert, reconciliation check |
| MITIGATE | Reduces severity if hazard occurs | Dose range check, confirmation dialog, undo action |
| TRANSFER | Transfers responsibility | User training, SOPs, workflow documentation |

### Control Question Protocol

For each HIGH or CRITICAL hazard:
1. "What technical control could PREVENT this hazard?"
2. "What monitoring/alerting could DETECT this hazard?"
3. "What safeguard could MITIGATE the impact if it occurs?"
4. "Are there non-technical controls (SOP, training) that TRANSFER residual risk?"

### Control ID Assignment

Controls are numbered: `C-{NNN}` (sequential, same watermark pattern as HAZ IDs).

### Control Template

```markdown
**C-{NNN}** — {Control title}

**Type:** {PREVENT | DETECT | MITIGATE | TRANSFER}
**Description:** {What the control does}
**Implemented by:** {API validation | Frontend warning | Monitoring alert | SOP}
**Reduces:** HAZ-{NNN} Likelihood from {L} to {L_reduced} / Severity maintained
```
