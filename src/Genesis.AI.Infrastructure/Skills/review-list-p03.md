# SKILL: review-list-p03
# Phase: P03 Architecture — Phase 0

## P03 Review List Management

### Creation (Phase 0)

Create `feedback/P03_REVIEW_LIST.md` at the start of Phase 0. This file tracks progress across all requirements.

```markdown
# Pipeline 03 Review List — {PRODUCT_NAME}
**Started:** {DATE} | **Last Updated:** {DATE}

| REQ-ID | Name | BDAT | ADRs | Failure Modes | Security | Written | Flag | Note |
|---|---|---|---|---|---|---|---|---|
| REQ-001 | {name} | ⏳ | | | | | | |
```

### Resume Rule

On session restart: read `feedback/P03_REVIEW_LIST.md`. The first row with a blank or `⏳` in the Written column and no 🚩 Flag is the resume point. Do not re-derive already-written requirements.

### Update Protocol

After each requirement is written to file, update the corresponding row:
- BDAT: `✅`
- ADRs: ADR numbers assigned (e.g. `ADR-- ADRs: ADR numbers assigned (e.g. `ADR-- ADRs: ADR numbers assigned (e.g. `ADR-- ADRs: ADR numbers assilocker was parked)

### Status Key

| Symbol | Meaning |
|--------|---------|
| `⏳` | In progress — currently being worked |
| `✅` | Complete — written to file |
| `↩️` | Revised — reopened and updated |
| blank | Not started |
| `🚩` | Flagged — blocked or needs follow-up |
