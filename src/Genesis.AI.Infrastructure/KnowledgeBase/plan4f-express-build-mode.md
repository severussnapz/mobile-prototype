# Plan 4f — Express Build Mode

## What it is

A fast-track pipeline mode for product builders doing PoCs, customer
site visits, or stakeholder demos. P01 runs a 5-question express
interview (10 mins), P02 generates a clickable prototype from the lean
spec (35 mins). Total: 45 mins from blank page to stakeholder demo.

After the demo, the user can promote to the full pipeline — P01 deepens
the specification, P03-P10 run normally.

## When to use

- Greenfield: describe what to build, for whom, what success looks like
- Brownfield: upload screenshots, describe what's not working
- PoC with customer on-site
- Internal stakeholder alignment before committing to full spec

## Mode flag

mode: express in the REQ file header. No DB changes. P02 reads this
flag from the artefact content to determine which path to take.

## P01 changes

Phase 0 adds a mode selection question before the classifier:
"Are you doing a full requirements session (detailed specification,
all dimensions, 1-2 hours) or an express build (prototype in 45 mins,
lightweight spec, refine later)?"

If express build: 5-question interview in one turn, images accepted,
REQ saved with mode: express flag, P02 ready immediately.

Express interview questions:
1. Who is this for? (persona, one sentence)
2. What problem does it solve? (job-to-be-done, one sentence)
3. What does success look like? (3 binary measures maximum)
4. What are the key screens or flows?
5. Brownfield only: What is not working? (upload screenshots)

Express REQ output structure:
- mode: express
- promoted: false
- Persona, Goal, Success Measures, Key Screens, Reference Images
- Status: Express spec — ready for P02. Promote to full spec via P01 after demo.

## P02 changes

When P02 reads mode: express flag:
- Skip deep REQ validation (no CHECKs required)
- Skip architecture cross-reference (no ARCH.md required)
- Build prototype directly from persona + goal + screens
- Inject uploaded reference images as visual context
- One phase — no gate between clarifying questions and build
- Target: working prototype in one generation pass

Brownfield: reference images injected into P02 generation prompt.
Agent describes what it sees and builds the improved version.

## Stakeholder loop

1. Demo to stakeholders
2. Return to P01 — say let's go deeper
3. P01 reads express REQ, sets promoted: true, runs full interview
4. P02 regenerates with deeper spec
5. P03-P10 run normally from that point

## What does not change

- REQ file format — express REQ is valid REQ, just shallower
- P02 tool set — same tools, PROTOTYPE ONLY banner, same guards
- Full pipeline — P03-P10 work exactly as today on promoted REQs
- Clinical safety — P06 still runs on promoted REQs

## Build order

Phase 1 (1 day): P01 express mode — mode selection, 5-question
  interview, image upload, express REQ template

Phase 2 (1 day): P02 express path — read mode flag, skip deep
  validation, inject images into generation prompt

Phase 3 (0.5 day): Brownfield — screenshot upload + vision input

Phase 4 (0.5 day): Promote to full spec — go deeper trigger,
  promoted flag, P01 continuation from express REQ

Total: 3 days

## Prerequisites

- Plan 4d PRs merged (main clean)
- Plan 4e Flow Spec (can run in parallel)
- PrototypeSingleFileEnabled: true in production (already done)
