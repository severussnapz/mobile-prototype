# SKILL: hazard-likelihood-scale
# Phase: P06 Clinical Safety — Phase 3

## Hazard Likelihood Scale

**Purpose:** Apply the DCB0129 5-point likelihood scale consistently.

### DCB0129 Likelihood Scale

| Level | Label | Description | Example |
|-------|-------|-------------|---------|
| 1 | Remote | Would only occur in exceptional circumstances | Complex multi-step failure requiring simultaneous errors |
| 2 | Unlikely | Could occur, but unlikely in normal use | Requires a specific rare edge case |
| 3 | Occasional | Could occur occasionally in normal use | Happens under specific but not unusual conditions |
| 4 | Likely | Likely to occur at some point | Could happen in routine use |
| 5 | Almost Certain | Will occur regularly if not controlled | Happens without specific action |

### Assignment Rule

Assign likelihood based on:
1. Frequency of the triggering condition in normal use
2. Whether the trigger requires user error, system error, or can occur passively
3. How easily a non-expert user could accidentally trigger the hazard

### Likelihood Justification Template

```markdown
**Likelihood:** {1-5} — {Label}
**Trigger frequency:** {How often does the triggering condition occur in normal use?}
**User action required:** {Active error / Passive / Automatic}
```
