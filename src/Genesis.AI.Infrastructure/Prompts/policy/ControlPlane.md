# EMIS Pipeline Control Plane

This document is the deterministic routing contract for the EMIS stage pipeline.
It does not replace stage agents. It decides whether a stage is runnable and what
must run next.

## Purpose

- Keep V1a -> V1g stage flow deterministic.
- Prevent routing based on subjective interpretation.
- Fail closed when prerequisites are missing.

## Stage Readiness Rules

| Stage | Required evidence before start | Blocking conditions |
|---|---|---|
| V1a | `manifest.md` exists (or user selected new product flow) | Missing scope selection / no requirements context |
| V1b | In-scope REQs include `## Evaluation Function Specification` + >=1 CHECK | Missing V1a carry-forward block |
| V1c | In-scope REQs include `## Architecture (Added by V1b)` | Missing V1b carry-forward block |
| V1d | In-scope REQs include `## Design (Added by V1c)` | Missing V1c carry-forward block |
| V1e* | In-scope REQs include `## PxD (Added by V1d)` | Missing V1d carry-forward block |
| V2 | `v2_local_normaliser.py` succeeded and `_gaps_manifest.json` exists per REQ | Missing V1e carry-forward block(s), `check_sdp_evidence.py` failed |
| V1f | `check_v2.py` and `preflight_v1f.py` both exit 0 | Any unassigned CHECK, unresolved open gap |
| V3 | `output/tasks/TASK-NNN.json` has non-empty `checks[]` and `pass_criteria` | Missing task file or malformed task file |
| V1g | GATE-3 + GATE-4 passed, `ops-config.json` present | Coding gates incomplete |

## Deterministic Next-Stage Selection

1. Evaluate readiness in order: V1a, V1b, V1c, V1d, V1e, V2, V1f, V3, V1g.
2. First stage that is not complete and is runnable becomes `next_stage`.
3. If stage is not runnable, emit `blocked` with exact missing artifact(s).
4. Never skip a stage unless delta-routing rules explicitly mark it `not_required`.

## Delta Routing Guardrails

A stage may be skipped only when all are true:

1. Required heading section from that stage already exists for every in-scope REQ.
2. No new control category was introduced (clinical, security, IG, observability).
3. No schema/API/UX boundary change requires that stage's authored outputs.
4. Skip decision is recorded in `feedback/VALUE_CHAIN.md` as explicit rationale.

## Required Output on Every Run

Every stage completion must append a carry-forward block to `feedback/VALUE_CHAIN.md`
using the shared stage output contract template.

## Blocking Severity

- `CRITICAL`: Patient safety, security, compliance, or data boundary missing -> stop.
- `MAJOR`: Required stage contract section missing -> stop.
- `MINOR`: Non-blocking metadata gap -> continue with warning and explicit carry-forward.
