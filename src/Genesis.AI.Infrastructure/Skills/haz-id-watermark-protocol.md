# SKILL: haz-id-watermark-protocol
# Phase: P06 Clinical Safety — Phase 0

## Hazard ID Watermark Protocol

**Purpose:** Maintain globally unique, monotonically increasing hazard IDs across all P06 sessions.

### Watermark Rules

1. On Phase 0 start: check `feedback/HAZ_ID_WATERMARK.md`
   - **Exists** → Read current watermark value `{N}`. Next new hazard ID = `HAZ-{N+1}`.
   - **Does not exist** → Current watermark = 0. Next new hazard ID = `HAZ-001`.

2. After each new hazard is created: increment the watermark and write back to `feedback/HAZ_ID_WATERMARK.md`.

3. **NEVER** reuse or skip a hazard ID. IDs are permanent once assigned.

4. If this is a REDUCED scope (delta): check existing hazard IDs in all `REQ-*.md` clinical safety sections. Watermark must start above the highest existing ID.

### Watermark File Format

```markdown
# Hazard ID Watermark

Current watermark: {N}
Last updated: {DATE}
Session: {PROJECT_CODE} P06 iteration {I}
```

### Hazard ID Format

`HAZ-{NNN}` — zero-padded to 3 digits. Example: `HAZ-001`, `HAZ-042`, `HAZ-100`.
