# SKILL: completeness-gate-p06
# Phase: P06 Clinical Safety — Phase 11.5

## Completeness Gate — P06

**Purpose:** Final verification before writing clinical safety sections to REQ files.

### Gate Checks

- [ ] Every requirement in P06_REVIEW_LIST.md has a HAZ entry (even if the entry is "No clinical hazards — {reason}")
- [ ] All hazard IDs are sequential with no gaps
- [ ] All hazard cards are complete (no ⏳ outstanding items)
- [ ] CSO sign-off recorded for all HIGH and CRITICAL hazards
- [ ] Hazard ID watermark file is up to date
- [ ] Decision log is complete

### If Gate Fails

List all failing checks. Create HIGH parking lot items for each. Do NOT write to REQ files until gate passes.

### Gate Pass Log

```
"P06 Completeness Gate: PASSED
- Requirements assessed: {N}
- Hazards documented: {M} (HAZ-001 to HAZ-{M})
- All ⏳ items resolved: Yes
- CSO sign-off: complete
- Writing clinical safety sections to REQ files..."
```
