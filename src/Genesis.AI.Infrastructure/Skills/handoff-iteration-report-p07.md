# SKILL: handoff-iteration-report-p07
# Phase: P07 Information Governance — Phase 7

## Handoff and Iteration Report — P07

**Purpose:** Create the P07 iteration report and hand off to P08 (Security).

### P07 Iteration Report

Save as `feedback/ITERATION_REPORT_P07_i{N}.md`:

```markdown
# P07 IG Iteration Report — {PROJECT_CODE} i{N}

**Date:** {DATE}
**Requirements assessed:** {N}
**Personal data types identified:** {list}
**Lawful bases confirmed:** {list}
**IG controls applied:** {IG-CTRL-NNN, ...}
**Parking lot items created:** {N}
**DPIA update required:** {Yes — {scope} / No}

## HIGH Priority Improvements for Next Session
1. {Improvement}

## Key Decisions Made
| Decision | Rationale |
|---------|----------|
| {Lawful basis: Public task} | {NHS system with public interest function} |
```

### Handoff to P08

Create `feedback/P07_P08_HANDOFF.md`:

```markdown
# P07 → P08 Handoff Notes

**Security-relevant IG findings:**
- Special category data types: {list}
- Third-party data transfers: {list — each needs security review in P08}
- Residual HIGH IG risks: {list or "None"}
- Data at rest encryption: {confirmed / to be confirmed in P08}

**P08 must address:**
- {Specific security controls required for these IG findings}
```
