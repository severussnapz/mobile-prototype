---
name: run-mode-routing-p03
description: 'Use this skill in P03 Phase 0. Reads the ROUTING CONTEXT block injected by the orchestrator and sets the execution path: full (new session), delta (additive mode for existing_extend services), or surgical (bugfix mode for existing_modify services).'
metadata:
  version: 1.0.0
  applyTo:
    - requirements
---

# Run Mode Routing — P03 Architecture

## Purpose

The routing context block is injected by the orchestrator before the session starts.
This skill defines how to read that block and set the correct execution path.
The agent executes the routing decision — it does not derive it.

## Three Run Modes

### FULL

**When:** `service_scope = new` for all requirements in scope, or no routing context present.

**Behaviour:** Run all phases in sequence. No skipping. All interview phases active.
This is the default when routing context is absent or service_scope is uniformly new.

### DELTA

**When:** Any requirement has `service_scope = existing_extend`.

**Behaviour:**
- Phase 1 (technology stack): Skip stack derivation for the extending service. Accept existing stack as given. Design NEW endpoints only — do not redesign existing contracts.
- Phase 2 (BDAT analysis): Run for new capabilities only. Mark reused capabilities as "extending [service name]" without re-analysing.
- All other phases: Run normally for new capabilities. Reference upstream artefacts for existing capabilities.

Announce delta mode at Phase 0 completion:
```
▶ Run mode: DELTA
Reason: REQ-[NNN] extends [service name] (existing_extend)
New endpoints only will be designed. Existing contracts are authoritative.
```

### SURGICAL

**When:** Any requirement has `service_scope = existing_modify`.

**Behaviour:**
- Scope is strictly limited to the modification described in the requirement.
- Do not redesign, refactor, or extend beyond the modification scope.
- Before any design: call `get_artefact` on the existing architecture/design artefacts for the service being modified.
- Flag any discovery that the modification would break an existing contract — CRITICAL parking lot item, hard stop.

Announce surgical mode at Phase 0 completion:
```
▶ Run mode: SURGICAL
Reason: REQ-[NNN] modifies [service name] (existing_modify)
Scope is strictly limited to the described modification.
Existing artefacts will be loaded before design begins.
```

## existing_use Services

`service_scope = existing_use` is not a run mode — it is a phase skip instruction.

For requirements where `service_scope = existing_use`:
- Skip Phases 1–11 for that service entirely.
- Document the dependency only: "This requirement uses [service name] as an existing dependency."
- Do not advance phase count for skipped phases — skip directly to the phase after the skipped range.

## Mixing Scopes in One Session

A single session may process requirements with different service_scope values.
Apply the appropriate mode per requirement, not per session.
Announce mode switches when transitioning between requirements:
```
▶ Switching to [mode] for REQ-[NNN]
```

## Swagger Present Routing

If `swagger_present = true` in the routing context:
- Phase 1 begins with a Swagger annotation pass, not a design interview.
- Load the Swagger artefact via `get_artefact`.
- Identify violations of API guardrails (AUTH-003/004, SEC-001/002, API-001/005/007).
- Design only gap endpoints — endpoints not covered by the existing Swagger spec.

If `swagger_present = false`: Full Phase 1 endpoint design interview for all endpoints.
