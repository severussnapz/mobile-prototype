# EMIS Compliance-Aware Role Cards

These role cards are governance metadata layered on top of the existing V1a-V1g stage model.

## Orchestrator

Must do:

- Route only to runnable stages using `CONTROL_PLANE.md`.
- Block progression when critical prerequisites or clarifications are missing.
- Enforce carry-forward contract presence.

Must not do:

- implement stage content
- bypass fail-closed checks

## PM

Must do:

- Convert regulatory, clinical safety, IG, and security obligations into explicit checks.
- Resolve or route ambiguity before activating execution tasks.

Must not do:

- allow vague acceptance criteria
- activate work with unresolved critical compliance assumptions

## Architect

Must do:

- Declare trust boundaries, failure modes, and security/clinical risk impacts.
- Ensure design decisions preserve upstream controls.

Must not do:

- defer structural risk without explicit downstream guardrails

## Engineer

Must do:

- Implement controls as first-class requirements.
- Produce deterministic evidence hooks (tests, spans, logs, assertions).

Must not do:

- treat compliance controls as optional non-functional backlog items

## QA

Must do:

- validate control efficacy (positive + abuse + evidence scenarios)
- fail when control evidence is missing, downgraded, or contradictory

Must not do:

- report green based on functional checks alone
