---
name: parking-lot
description: 'Use this skill in all P03–P08 stages. Defines the two-tier parking lot structure, tool call protocol, resolution lifecycle, and 10-item cap enforcement.'
metadata:
  version: 1.0.0
  applyTo:
    - requirements
---

# Parking Lot

## Purpose

The parking lot captures topics that arise during an interview phase but cannot be resolved immediately — either because they are out of scope for the current phase, require stakeholder input, or are blocked by an upstream answer.

## Two-Tier Structure

**Tier 1 — Tracked (tool call):**
Items that must be resolved before the requirement moves to normalisation.
Use the `add_parking_lot_item` tool. These appear in the project-level view.

**Tier 2 — Review list (text note):**
Items that are advisory or informational. Captured in the stage review list file
(e.g. `feedback/P03_REVIEW_LIST.md`). Not tracked as tool-managed items.

## Priority Rules

| Priority | When to assign |
|----------|---------------|
| 🔴 CRITICAL | Blocks completion of the current stage. Unresolved = cannot advance. |
| 🟡 HIGH | Must be resolved before normalisation. Will degrade output quality if skipped. |
| 🔵 MEDIUM | Should be resolved before implementation. Advisory, not a blocker. |

## 10-Item Cap

If the parking lot reaches 10 open CRITICAL or HIGH items, stop the interview.
Do not add an 11th item. Instead:
1. Announce the cap has been reached.
2. Present the full list to the human.
3. Wait for the human to resolve or defer at least 3 items before continuing.

## Tool Call Protocol

Before calling `add_parking_lot_item`:
1. State the item in plain language in your response.
2. Explain why it cannot be resolved in this phase.
3. Then emit the tool call.

Never emit a tool call silently. The human should always see what is being parked before it is stored.

## Resolution Lifecycle

An item moves through: **open → resolved / deferred**.

When an earlier-parked item becomes answerable in a later phase:
1. Re-surface it explicitly: "Earlier we parked [topic]. Now that we've established [context], I can address it."
2. Provide the resolution.
3. Call `resolve_parking_lot_item`.

## Deduplication

Before adding a new item, check whether an equivalent item already exists in the current parking lot.
Duplicate items inflate the count and confuse resolution tracking. If equivalent: update the existing item's priority if the new context warrants escalation.
