---
name: pre-fill-confidence-markers
description: 'Use this skill in P06, P07, P08 when presenting pre-filled content to a human reviewer. Defines CONFIRMED vs PROPOSED labelling rules and the prohibition on presenting pre-fills as confirmed without explicit source verification.'
metadata:
  version: 1.0.0
  applyTo:
    - requirements
---

# Pre-Fill Confidence Markers

## Purpose

When the orchestrator or agent pre-fills content from prior artefacts or standard skill baselines, the human reviewer must know the confidence level of each item. This prevents rubber-stamping of unverified AI-derived content.

## Two Marker Types

### CONFIRMED

Use when the pre-filled value comes from a **verified prior source**:
- A human decision recorded in a prior phase of this same stage
- A value explicitly confirmed by the relevant reviewer (CSO, DPO, security lead) in a prior iteration
- A value from a completed and signed-off upstream stage artefact

Format: `[CONFIRMED — source: P03 ADR-001, confirmed by architect 2026-05-14]`

### PROPOSED

Use when the pre-filled value is **AI-derived** from context, standard patterns, or inference:
- Derived from prior stage artefacts without explicit human confirmation
- Derived from EMIS-X standard skill content
- Inferred from requirement text

Format: `[PROPOSED — review required before acceptance]`

## Rule 1 — Every pre-filled item must carry a marker

No pre-fill may be presented without either CONFIRMED or PROPOSED.
An unmarked pre-fill is treated as PROPOSED for review purposes.

## Rule 2 — Never downgrade PROPOSED to CONFIRMED without evidence

Do not change a PROPOSED marker to CONFIRMED based on:
- The reviewer saying "looks fine" to a batch
- The reviewer not objecting
- Model inference that the reviewer would confirm

CONFIRMED requires an explicit, specific acceptance of the item by the authorised reviewer.

## Rule 3 — Batch presentations must be exception-oriented

When presenting a pre-filled batch (e.g. OWASP baseline, ASVS mapping):
- Lead with: "The following items are pre-filled [PROPOSED]. Please identify any exceptions — items you would change, remove, or escalate."
- Do not ask the reviewer to confirm each item individually.
- Any item not raised as an exception is treated as accepted, not confirmed. It remains PROPOSED in the artefact until a formal sign-off phase.

## Rule 4 — PROPOSED items in final artefacts require a reviewer pass

A final artefact containing PROPOSED items CANNOT be handed off with Status: READY.
All PROPOSED items must be resolved to CONFIRMED or removed before Status: READY is valid.
