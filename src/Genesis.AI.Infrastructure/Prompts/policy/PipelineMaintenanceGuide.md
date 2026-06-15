# Pipeline Maintenance Guide
Version: pipeline-10-stage-canonical
Audience: Platform maintainers and pipeline engineers
Note: This file is for human maintainers only. It is NOT referenced by agent prompts at runtime.

This guide explains how to safely make changes to agents, stage prompts, scripts, and policy files without breaking the pipeline.

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

---

## 1. End-to-End Pipeline Relationship Model

The pipeline is additive. Each stage appends structure and evidence; no stage overwrites prior stage intent.

```
01 Requirements
   → 02 Prototype (optional validation loop)
   → 03 Architecture
   → 04 Design
   → 05 PxD
   → 06 Clinical Safety
   → 07 Information Governance
   → 08 Security
   → 09 Normalisation (run v2_local_normaliser, then V2 gap-fill)
   → 10 Planning (preflight, intake chat, EM review gate, split tasks)
   → Coding Agent (TASK-NNN.json)
```

---

## 2. Stage-to-Stage Contract Summary

| Stage | Primary output | Next stage depends on |
|---|---|---|
| 01 Requirements | manifest.md, REQ-*.md with CHECKs | Pipeline 02/03 headings + CHECK presence |
| 02 Prototype | prototype/index.html | Validation evidence or explicit skip |
| 03 Architecture | `## Architecture (Added by Pipeline 03)` per REQ | Contract and schema decisions |
| 04 Design | `## Design (Added by Pipeline 04)` per REQ | UX and interaction constraints |
| 05 PxD | `## PxD (Added by Pipeline 05)` per REQ | Safety/security/IG controls |
| 06 Clinical Safety | Clinical Safety sections, HAZARD-REGISTRY.md | Hazard controls + CHECKs |
| 07 Information Governance | IG sections, PR1625_DPIA_DATA.json | Lawful basis + DPIA evidence |
| 08 Security | Security sections, SECURITY_ASSURANCE_DATA.json, SDP_EVIDENCE.json | Attack-vector coverage + CHECKs |
| 09 Normalisation | output/{REQ_ID}/*.json, cross_cutting JSON, _gaps_manifest.json | All per-REQ JSON non-empty |
| 10 Planning | Task_Plan.md, tasks_data.json, TASK-NNN.json, task_index.json | All gate conditions passed |

---

## 3. Agent/Prompt File Locations

| Stage | Prompt file |
|---|---|
| 01 | Pipeline01RequirementsDiscovery.md |
| 02 | Pipeline02Prototype.md |
| 03 | Pipeline03Architecture.md |
| 04 | Pipeline04Design.md |
| 05 | Pipeline05Pxd.md |
| 06 | Pipeline06ClinicalSafety.md |
| 07 | Pipeline07InformationGovernance.md |
| 08 | Pipeline08Security.md |
| 09 | Pipeline09Normalisation.md |
| 10 | Pipeline10Planning.md |

Policy files (all stages reference these via Shared Governance section):
- `policy/ControlPlane.md`
- `policy/CorePolicy.md`
- `policy/RoleCards.md`
- `policy/AgentBaseline.md`
- `policy/PipelineContract.md`
- `policy/StageOrchestration.md`

---

## 4. How to Safely Modify a Stage Prompt

1. Read the current prompt and the `StageOrchestration.md` contract for that stage.
2. Identify what inputs the stage expects — do not break the pre-start check section.
3. Identify what outputs the stage produces — do not change output headings without updating `PipelineContract.md` and `StageOrchestration.md`.
4. Keep the canonical runtime contract block (`## 0. Canonical Runtime Contract`) unchanged unless the stage number itself changes.
5. Add new content in new named sections. Do not move or delete existing sections.
6. Test by running the changed prompt against a real project before committing.

---

## 5. How to Safely Add a New Policy File

1. Create the file under `policy/`.
2. Add an explicit reference to it in the `Shared Governance Artefacts (Mandatory)` section of every stage prompt that should consume it.
3. Do not add policy files that are only for human reading — keep them out of the agent reference list to avoid polluting agent context.
4. Update this guide's policy file table in section 3.

---

## 6. Process Script Dependency Matrix

| Script | Stage | Trigger | Input | Output |
|---|---|---|---|---|
| v2_local_normaliser | Stage 09 | Product button: Extract Requirements | requirements/*.md | output/{REQ_ID}/*.json, _gaps_manifest.json |
| preflight_v1f | Stage 10 | Product button: Run Preflight | output/*.json | output/planning/PREFLIGHT_STATUS.json |
| split_tasks | Stage 10 | Product button: Generate Task Files | output/planning/tasks_data.json | output/tasks/TASK-NNN.json, task_index.json, SPLIT_STATUS.json |
| build_hazard_log | Stage 06 | Product button: Export Hazard Log | HAZARD-REGISTRY.md | feedback/HAZARD_LOG_*.xlsx |
| generate_pr1625_docx | Stage 07 | Product button: Export DPIA | PR1625_DPIA_DATA.json | feedback/PR1625_DPIA_*.docx |
| generate_security_review | Stage 08 | Product button: Export Security Review | SECURITY_ASSURANCE_DATA.json + SDP_EVIDENCE.json | feedback/SECURITY_REVIEW_REPORT.xlsx |

---

## 7. What NOT to Change

1. Do not change the stage code values in `canonical_stage_dictionary` without updating all 10 stage prompts.
2. Do not rename `## {Section} (Added by Pipeline NN)` headings without updating `StageOrchestration.md` and `PipelineContract.md`.
3. Do not remove the fail-closed rules in `CorePolicy.md`.
4. Do not change `PipelineContract.md` carry-forward expectations without running a value-chain integrity check against a real project.
5. Do not add new required inputs to a stage without populating the upstream stage that produces them.
6. Do not rename the GENESIS marker strings (`<!-- GENESIS:STYLES -->`, `<!-- GENESIS:NAV -->`, `<!-- GENESIS:SCREENS -->`, `<!-- GENESIS:DATA -->`, `<!-- GENESIS:APP -->`) — they are load-bearing and matched by `PrototypeAssemblyService`.
7. Do not change the `prototype/fragments/` path prefix or the `_shell.html`, `_styles.css`, `_app.js`, `data.js` fragment naming convention without updating `PrototypeAssemblyService` constants.
