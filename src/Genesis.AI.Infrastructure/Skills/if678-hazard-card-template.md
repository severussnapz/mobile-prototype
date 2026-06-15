# SKILL: if678-hazard-card-template
# Phase: P06 Clinical Safety — Phase 7 (AUTO)

## IF678 Hazard Card Template

**Purpose:** Auto-generate the formal DCB0129 hazard record for each identified hazard.

> 🤖 **AUTO PHASE:** This phase runs automatically after Phase 6 (residual risk) is confirmed for each hazard. No user input required per hazard.

### Hazard Card Template (IF678 format)

```markdown
## {HAZ-NNN}: {Hazard Title}

| Field | Value |
|-------|-------|
| Hazard ID | {HAZ-NNN} |
| Requirement | REQ-{NNN} — {Name} |
| Hazard description | {Plain-language description} |
| HAZOP category | {WRONG / OMISSION / DELAY / etc.} |
| Initial severity | {1-5} — {Label} |
| Initial likelihood | {1-5} — {Label} |
| Initial risk | {Score} — {Level} |
| Controls | {C-NNN: description; C-MMM: description} |
| Residual likelihood | {1-5} — {Label} |
| Residual risk | {Score} — {Level} |
| Acceptable | {Yes / Yes with monitoring / No — escalated} |
| CSO review | {⏳ Pending / ✅ {Name} on {DATE}} |
```

### Auto-Log

After generating a hazard card: "✅ Hazard card {HAZ-NNN} written. Moving to next hazard."
