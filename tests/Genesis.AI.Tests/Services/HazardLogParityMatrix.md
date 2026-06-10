# Hazard Log IF678 Parity Matrix (C# vs Python)

This matrix defines acceptance criteria for the existing C# hazard-log XLSX builder, aligned to:
- scripts/build_hazard_log_from_registry.py
- templates/if678-clinical-safety-hazard-log-increment.xlsm behaviour

## Row Rules

1. One data row per cause.
2. Hazard-level fields written only on the first cause row for a hazard.
3. Hazard-level columns merged across all cause rows for that hazard, except Column J.
4. Column J is cause-specific and must never be merged.
5. Data rows start at row 5.
6. Sheet freeze pane is at row 5 (rows 1-4 frozen).

## Column Rules (A-Y)

| Col | Rule | Source |
|---|---|---|
| A | Hazard count (1-based sequential) on first cause row only; merged for multi-cause hazard | Python build_excel |
| B | Date Added display value on first cause row only; merged for multi-cause hazard | Python build_excel |
| C | Requirement reference on first cause row only; merged for multi-cause hazard | Python build_excel |
| D | Product module value on first cause row only; merged for multi-cause hazard | Python build_excel |
| E | Hazard reference ID on first cause row only; merged for multi-cause hazard | Python build_excel |
| F | Hazard area left blank (not populated from registry) on first cause row only; merged for multi-cause hazard | Python build_excel |
| G | Hazard description on first cause row only; merged for multi-cause hazard | Python build_excel |
| H | Potential clinical impact on first cause row only; merged for multi-cause hazard | Python build_excel |
| I | Cause text on every cause row | Python build_excel |
| J | Existing controls selected per cause using deterministic fallback algorithm; never merged | Python pick_existing_controls_for_cause |
| K | Initial severity = [TBD] on first cause row only; merged for multi-cause hazard | Python build_excel |
| L | Initial likelihood = [TBD] on first cause row only; merged for multi-cause hazard | Python build_excel |
| M | Initial risk = [TBD] on first cause row only; merged for multi-cause hazard | Python build_excel |
| N | HIT Design control descriptions for cause (joined by newline) | Python controls_for_category |
| O | HIT Design evidence for cause (joined by newline) | Python controls_for_category |
| P | Training control descriptions for cause | Python controls_for_category |
| Q | Training evidence for cause | Python controls_for_category |
| R | Business Process control descriptions for cause | Python controls_for_category |
| S | Business Process evidence for cause | Python controls_for_category |
| T | Customer controls from first cause row only; merged for multi-cause hazard | Python build_excel |
| U | Residual severity = [TBD] on first cause row only; merged for multi-cause hazard | Python build_excel |
| V | Residual likelihood = [TBD] on first cause row only; merged for multi-cause hazard | Python build_excel |
| W | Residual risk = [TBD] on first cause row only; merged for multi-cause hazard | Python build_excel |
| X | Status on first cause row only; merged for multi-cause hazard | Python build_excel |
| Y | Additional comments on first cause row only; merged for multi-cause hazard | Python build_excel |

## Column J Existing Controls Selection Algorithm

Given hazard-level existing-controls text and cause row context:

1. Split existing-controls text into sentence fragments using regex boundary after period + whitespace.
2. If HIT evidence contains one or more CHECK references (CHECK n), select fragments containing any matching CHECK token.
3. Else score each fragment by overlap of 4-char word roots with cause text roots; select highest score when > 0.
4. Else fallback: first cause row gets the full existing-controls text; subsequent cause rows get blank.

## Merge Columns for Multi-Cause Hazards

Expected merged columns for each hazard block:
A, B, C, D, E, F, G, H, K, L, M, T, U, V, W, X, Y

Not merged:
I, J, N, O, P, Q, R, S
