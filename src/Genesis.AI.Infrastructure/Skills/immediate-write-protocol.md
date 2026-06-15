# SKILL: immediate-write-protocol
# Phase: P03 Architecture — Phase 2

## Immediate Write Protocol

> 📝 **WRITE IMMEDIATELY — MANDATORY:** As soon as the user confirms "Correct" for each requirement's BDAT analysis, write the `## Architecture (Added by Pipeline 03)` section (including `### BDAT Analysis` and any ADRs confirmed so far) to that requirement's file **before** proceeding to the next requirement.

Do NOT accumulate writes. Each confirmation = one file write.

After writing, log: `"✅ REQ{N} Architecture section written to file."`

Then update `feedback/P03_REVIEW_LIST.md` to mark BDAT as `✅` for that row.

## What to Write

The Architecture section at minimum must contain:
- `### BDAT Analysis` — Business, Data, Application (with v3_agents), Technology
- `### Architecture Decision Records` — all ADRs assigned to this requirement so far
- `### Service Classification` — one entry per service touched

## Phase 12 Relationship

Phase 12 is a **verification and gap-fill pass only**. Cross-cutting sections (Platform Boundaries, Failure Modes, Security, etc.) that were not known at BDAT confirmation time are added in Phase 12. Do NOT wait until Phase 12 to write the BDAT section.
