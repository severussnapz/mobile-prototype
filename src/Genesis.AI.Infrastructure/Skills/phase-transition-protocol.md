---
name: phase-transition-protocol
description: 'Use this skill in all pipeline stages. Defines mandatory advance_phase tool call rules, phase announcement format, and prohibition on silent phase skipping.'
metadata:
  version: 1.0.0
  applyTo:
    - requirements
---

# Phase Transition Protocol

## Rule 1 — Mandatory tool call

Every phase transition MUST emit an `advance_phase` tool call.
Do not transition between phases without calling the tool.
The tool call is what the orchestrator uses to track current state — a text announcement without the tool call is invisible to the system.

## Rule 2 — Pre-transition summary

Before calling `advance_phase`, always present a phase completion summary (see `interview-discipline` skill Rule 4 format).
Do not call the tool without a preceding summary.

## Rule 3 — Announcement format

After `advance_phase` succeeds, announce the new phase with this exact format:

```
---
## Phase [N]: [Phase Name]

[One sentence describing the purpose of this phase and what will be established.]
```

The `---` separator is mandatory. It visually delineates phase boundaries for human reviewers.

## Rule 4 — No silent skipping

Do not skip a phase without announcement, even when routing context marks it as fast-tracked or auto-derivable.

For fast-tracked phases, use this format instead:

```
---
## Phase [N]: [Phase Name] ⚡ Fast-tracked

[Pre-filled from: source]. Presenting for confirm/exception:

[pre-filled content]

Any corrections or exceptions before I advance?
```

Then call `advance_phase` after receiving confirmation (or after a reasonable timeout if the human does not respond within the same turn).

## Rule 5 — Phase 0 is always Phase 0

Every stage begins at Phase 0 (context loading). Never advance past Phase 0 without completing context loading, manifest read, and routing context acknowledgement.
