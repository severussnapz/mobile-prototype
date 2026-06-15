# SKILL: ig003-gate-p03
# Phase: P03 Architecture — Phase 2

## IG-003 Lawful Basis Gate (P03)

This gate applies to every requirement involving patient or clinical data during the BDAT Data analysis.

### Check

After completing the Data sub-section for a requirement, check: does Dimension 2 of this requirement contain `IG-003: Lawful Basis Declaration [UNVERIFIED]`?

### If UNVERIFIED is Found

Ask: "Has the lawful basis under UK GDPR Article 9(2) been confirmed for this requirement? (Legal/IG review required)"

- **If YES** → Update the `[UNVERIFIED]` tag to `[CONFIRMED — {date} by {role}]` in the Architecture section before proceeding.
- **If NO** → Add `[BLOCKED — legal review required before Pipeline 04 — owner: {IG lead}]` to the Architecture BDAT Data sub-section. Add a 🔴 CRITICAL parking lot item.

### Hard Rule

Do NOT silently carry `[UNVERIFIED]` forward without flagging it to the user. An unresolved IG-003 UNVERIFIED is a pipeline blocker — Pipeline 04 cannot proceed for that requirement.
