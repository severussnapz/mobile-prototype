# SKILL: haz-id-assignment-rules
# Phase: P06 Clinical Safety — Phase 1

## Hazard ID Assignment Rules

**Purpose:** Ensure consistent, unique hazard ID assignment.

### Rules

1. Check `feedback/HAZ_ID_WATERMARK.md` for current watermark (set in Phase 0).
2. Assign IDs sequentially: `HAZ-{watermark+1}`, `HAZ-{watermark+2}`, etc.
3. After each new ID is assigned: update the watermark file immediately. Do NOT batch watermark updates.
4. If a hazard is determined NOT applicable: do NOT assign an ID. Log the HAZOP category and reason.
5. IDs are permanent. If a hazard is later determined to be a duplicate: merge — note "HAZ-NNN superseded by HAZ-MMM" in both the hazard card and the watermark log.

### ID Format

`HAZ-NNN` — 3-digit zero-padded. `HAZ-001` through `HAZ-999`. If > 999 hazards (extremely unusual): escalate to `HAZ-1000`.

### Tracking

After Phase 1 complete, log: "Hazard identification complete. {N} hazards identified: {HAZ-001} through {HAZ-NNN}."
