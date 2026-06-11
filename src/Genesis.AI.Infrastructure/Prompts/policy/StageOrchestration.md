# Pipeline Stage Orchestration Map
Version: pipeline-10-stage-canonical
Owner: Genesis AI Platform
Status: Authoritative runtime reference

This file is the single source of truth for how the 10-stage AI pipeline is sequenced. Every stage agent MUST read this file and verify its position, inputs, and outputs before proceeding.

If any stage receives inputs that do not match what this file defines, it must STOP and report a stage-contract mismatch.

---

## Canonical Stage Naming Map

This table is authoritative. V-labels are used in policy and agent files. P-labels are used in stage prompt filenames and UI display. When referencing a stage, always use the P-label in prompt filenames and the V-label in policy routing.

| V-label | P-label |
|---|---|
| V1a Requirements | P01 Requirements Discovery |
| V1a Prototype | P02 Prototype |
| V1b | P03 Architecture |
| V1c | P04 Design |
| V1d | P05 PxD |
| V1e Clinical Safety | P06 Clinical Safety |
| V1e Information Governance | P07 Information Governance |
| V1e Security | P08 Security |
| V2 | P09 Normalisation |
| V1f | P10 Planning |

> The Stage Contract Table below uses P-labels. The table above is the cross-reference for V-label users.

---

## 10-Stage Pipeline Overview

```
01 Requirements → 02 Prototype → 03 Architecture → 04 Design → 05 PxD
→ 06 Clinical Safety → 07 Information Governance → 08 Security
→ 09 Normalisation → 10 Planning → Coding Agent
```

Each stage is additive. No stage overwrites prior stage output. Every stage appends to `feedback/VALUE_CHAIN.md`.

---

## Stage Contract Table

| Stage | Agent Prompt | Primary Inputs | Primary Outputs | Next Stage Trigger |
|---|---|---|---|---|
| 01 Requirements | Pipeline01RequirementsDiscovery.md | User interviews, manifest.md | requirements/REQ-*.md, manifest.md, feedback/VALUE_CHAIN.md | All REQs have Evaluation Function Spec + CHECKs |
| 02 Prototype | Pipeline02Prototype.md | REQ-*.md from Stage 01 | prototype/index.html | Prototype validated or explicitly skipped |
| 03 Architecture | Pipeline03Architecture.md | REQ-*.md, manifest.md | `## Architecture (Added by Pipeline 03)` per REQ, ADRs | All REQs have Architecture section |
| 04 Design | Pipeline04Design.md | REQ-*.md with Architecture sections | `## Design (Added by Pipeline 04)` per REQ, API contracts, schemas | All REQs have Design section |
| 05 PxD | Pipeline05Pxd.md | REQ-*.md with Design sections | `## PxD (Added by Pipeline 05)` per REQ, user flows, component specs | All REQs have PxD section |
| 06 Clinical Safety | Pipeline06ClinicalSafety.md | REQ-*.md with PxD sections | `## Clinical Safety (Added by Pipeline 06)` per REQ, HAZARD-REGISTRY.md, IF678 export | All hazards have CSO-approved controls + CHECKs |
| 07 Information Governance | Pipeline07InformationGovernance.md | REQ-*.md with prior sections, PR1625 policy docs | `## Information Governance (Added by Pipeline 07)` per REQ, output/PR1625_DPIA_DATA.json | All REQs have IG section + DPIA data |
| 08 Security | Pipeline08Security.md | REQ-*.md with Architecture + IG sections, policy docs | `## Security (Added by Pipeline 08)` per REQ, output/SECURITY_ASSURANCE_DATA.json, output/SDP_EVIDENCE.json | All REQs have Security section + assurance data |
| 09 Normalisation | Pipeline09Normalisation.md | REQ-*.md with all prior sections | output/{REQ_ID}/*.json, output/cross_cutting/*.json, output/_gaps_manifest.json | Normaliser run succeeded + V2 gap-fill complete |
| 10 Planning | Pipeline10Planning.md | output/*.json from Stage 09, intake answers | output/planning/Task_Plan.md, output/planning/tasks_data.json, output/tasks/TASK-NNN.json, output/tasks/task_index.json | All gate conditions passed + EM approved |

---

## Per-Stage Action Buttons (Product UI)

| Stage | Primary Process Buttons | Secondary Chat Action |
|---|---|---|
| 01 Requirements | — | Start / Continue conversation |
| 02 Prototype | Preview Prototype | Start / Continue conversation |
| 03 Architecture | — | Start / Continue conversation |
| 04 Design | — | Start / Continue conversation |
| 05 PxD | — | Start / Continue conversation |
| 06 Clinical Safety | Export Hazard Log (Excel .xlsx) | Start / Continue conversation |
| 07 Information Governance | Export DPIA (Word .docx) | Start / Continue conversation |
| 08 Security | Export Security Review (Excel .xlsx) | Start / Continue conversation |
| 09 Normalisation | Extract Requirements, Verify V2 Complete | Start / Continue conversation |
| 10 Planning | Run Preflight, Generate Task Plan, Approve Plan, Generate Task Files | Start / Continue conversation (intake) |

---

## Stage 09 Normalisation Detail

Stage 09 is process-driven, not chat-driven. Steps in order:

1. User clicks **Extract Requirements** — runs v2_local_normaliser internally.
2. Outputs written to:
   - `output/{REQ_ID}/checks.json`
   - `output/{REQ_ID}/hazards.json`
   - `output/{REQ_ID}/schema.json`
   - `output/{REQ_ID}/interfaces.json`
   - `output/{REQ_ID}/components.json`
   - `output/{REQ_ID}/observability.json`
   - `output/cross_cutting/traceability.json`
   - `output/cross_cutting/dependency_graph.json`
   - `output/cross_cutting/last_extracted.json`
   - `output/_gaps_manifest.json`
3. If `_gaps_manifest.json` shows gaps, user opens V2 gap-fill chat.
4. V2 gap-fill MUST NOT write to `traceability.json` or `dependency_graph.json`.
5. User clicks **Verify V2 Complete** — runs preflight/gate check.
6. Stage completion enabled only when gate passes.

---

## Stage 10 Planning Detail

Stage 10 is hybrid: short intake chat then process actions. Steps in order:

1. User opens chat — V1f agent asks 5 intake questions:
   - Team size
   - Timeline
   - Work allocation approach
   - Parallelism preference
   - Priority and scope emphasis
2. V1f builds task plan using output from Stage 09 + intake answers.
3. V1f presents `Task_Plan.md` for EM review. **STOPS and waits.**
4. EM reviews and approves in chat.
5. V1f outputs one `tasks_data.json` block.
6. User clicks **Approve Plan** — locks the approval version.
7. User clicks **Generate Task Files** — splits tasks_data into TASK-NNN.json files.

Gate conditions for Stage 10 completion (all 8 must pass):
1. `output/planning/PREFLIGHT_STATUS.json` status=passed
2. `output/planning/Task_Plan.md` exists
3. `output/planning/tasks_data.json` exists and is valid JSON with `tasks[]`
4. `output/planning/EM_APPROVAL.json` exists and is not stale
5. `output/tasks/SPLIT_STATUS.json` status=passed
6. `output/tasks/task_index.json` exists
7. At least one `output/tasks/TASK-*.json` file exists
8. No duplicate task IDs or CHECK assignment conflicts

---

## Cross-Cutting Artifacts

These files are produced once and must not be overwritten by any stage agent:

| Artifact | Owner Stage | Must not be modified by |
|---|---|---|
| `output/cross_cutting/traceability.json` | Stage 09 (normaliser) | V2 gap-fill agent |
| `output/cross_cutting/dependency_graph.json` | Stage 09 (normaliser) | V2 gap-fill agent |
| `feedback/VALUE_CHAIN.md` | All stages (append only) | Any stage (append only, never overwrite) |
| `output/planning/EM_APPROVAL.json` | Stage 10 (approve-em-review action) | Chat agent |

---

## Fail-Closed Rules

1. Any stage that finds its required input missing must STOP and state what is missing.
2. Any stage that finds the prior stage carry-forward block absent must STOP and ask user to re-run the prior stage.
3. No stage may silently drop a control, hazard, CHECK, gap, or requirement.
4. V2 gap-fill must not write to cross-cutting files.
5. Stage 10 split must not proceed unless EM approval is current (not stale).
6. A stage prompt that references a stage code not in the canonical stage dictionary must STOP.
