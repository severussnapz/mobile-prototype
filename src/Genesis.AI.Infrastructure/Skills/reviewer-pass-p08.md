# SKILL: reviewer-pass-p08
# Phase: P08 Security — Phase 6

## Reviewer Pass — P08

**Purpose:** Final review of all security sections before handoff.

### Review Checklist

- [ ] Every endpoint has `[Authorize(Policy = "...")]` (SEC-001)
- [ ] No raw SQL / string-interpolated queries (SEC-002)
- [ ] No PHI in log statements (SEC-003)
- [ ] No hardcoded secrets or credentials (SEC-004)
- [ ] All threats have at least one control
- [ ] No CRITICAL unmitigated threats
- [ ] OWASP A01 (access control) checked for every write endpoint
- [ ] P07 handoff items addressed

### Common Mistakes

- Missing `[Authorize]` on a new endpoint added after initial design
- Logging `nhsNumber` directly: `_logger.LogInformation("{NhsNumber}", nhsNumber)` — must use patient ID reference
- Using `string.Format()` in EF LINQ queries — must be parameterised
- Missing rate limiting on password-equivalent endpoints

### If a Section Fails Review

Create a 🔴 CRITICAL parking lot item. Do NOT mark the row as reviewed until fixed.
