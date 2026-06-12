# Agent Baseline — EMIS Pipeline

Single source of truth for the canonical agent set in this package. Agents live
in `agents/EMIS/`; the scripts they depend on live in `scripts/`. This manifest
records which file is canonical for each stage, its internal version, and the
scripts that bracket it.

> **Status:** clean baseline established as a self-contained package. The V1a–V1e
> agents are the corrected/newer versions sourced from the Docman delivery (which
> had moved ahead of the dev-utils copies); V2 is the v10 skeleton-aware refactor
> from dev-utils; V1f is the dev-utils script-based spine with the Docman PF-008
> clinical-safety gate and `V1F_REVIEW_LIST.md` tracker grafted on. Filenames are
> **suffix-free**. See [DIFF_REPORT.md](DIFF_REPORT.md) for the per-agent
> provenance and the reasoning behind each choice.

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

## Filename convention

Canonical agents use **version-free filenames** (`V1f_planning.agent.md`). The
internal version lives in each agent's `**Version:**` frontmatter line and in the
table below. This is the convention; the rename is complete in this package.

---

## Shared governance layer (A+++)

All stage and coding agents now align to these shared artifacts:

- `CONTROL_PLANE.md` (deterministic stage routing and blocking)
- `agents/EMIS/CORE_POLICY.md` (shared fail-closed rules)
- `templates/stage-output-contract.template.md` (uniform stage evidence block)
- `templates/clarification-artifact.template.md` (clarification lifecycle)
- `ROLE_CARDS.md` (compliance-aware role boundaries)

Baseline evidence and take-stock commands are tracked in `SDLC_BASELINE.md`.

---

## Canonical pipeline agents

| Stage | File (`agents/EMIS/`) | Internal ver | Provenance | Scripts that bracket it |
|---|---|---|---|---|
| V1a Requirements | `V1a_requirements.agent.md` | v7 | Docman (V1a→V1e hazard separation) | — |
| V1a Prototype | `V1a_prototype.agent.md` | v9 (alt) | dev-utils alternative (+ Option 3 in-app) | — |
| V1b Architecture | `V1b_architecture.agent.md` | v7 | Docman (+ `V1B_REVIEW_LIST.md`) | — |
| V1c Design | `V1c_design.agent.md` | v7 | Docman (+ `V1C_REVIEW_LIST.md`) | — |
| V1d PxD | `V1d_pxd.agent.md` | v8 | Docman (+ `V1D_REVIEW_LIST.md` + decision log) | — |
| V1e Clinical Safety | `V1e_clinical_safety.agent.md` | **v11** | Docman (Rule 8 plain language, IF678 cards) | `build_hazard_log_from_registry.py` (on demand) |
| V1e Information Governance | `V1e_information_governance.agent.md` | v1 | pipeline baseline (new) | in-agent reviewer pass writes `feedback/IG_REVIEW_REPORT.md`; must produce `output/PR1625_DPIA_DATA.json`; `generate_pr1625_docx.py` run on demand |
| V1e Security | `V1e_security.agent.md` | v1 | pipeline baseline (new) | in-agent reviewer pass writes `feedback/SECURITY_REVIEW_REPORT.md`; must produce `output/SECURITY_ASSURANCE_DATA.json`; `generate_security_review_report.py` run on demand |
| V1e Generic | `V1e_generic.agent.md` | v1-nonemis | dev-utils (Docman has none) | — |
| V2 Normalisation | `V2_normalisation.agent.md` | v10 | dev-utils (skeleton-aware refactor) | `v2_local_normaliser.py` (before), `check_v2.py` (Gate 2), `preflight_v1f.py` (gate after) |
| V1f Planning | `V1f_planning.agent.md` | v10 + PF-008 | **merged** (dev-utils spine + Docman PF-008 + review list) | `split_tasks.py` (after) |
| V1g Operations | `V1g_operations.agent.md` | v1 | dev-utils (Docman has none) | — |

---

## V3 coding agents

| Agent | File | Role |
|---|---|---|
| EMIS-X API Engineer | `EMIS-X_API_ENGINEER.agent.md` | Backend tasks, layers 0–3 |
| EMIS-X Webapp Engineer | `EMIS-X_WEBAPP_ENGINEER.agent.md` | Frontend tasks, layers 4–7 |
| EMIS Web Engineer | `EMIS_WEB_ENGINEER.agent.md` | .NET Framework monolith (outside the API/webapp flow) |
| EXA Data Engineer | `EXA_DATA_ENGINEER.agent.md` | Data pipelines (outside the API/webapp flow) |

Reviewer model in this baseline:
- Clinical Safety, Information Governance, and Security use in-agent two-phase
   producer+reviewer workflows.
- No standalone IG/Security reviewer agents are required.

---

## Scripts inventory (`scripts/`)

| Script | Used by | Purpose |
|---|---|---|
| `v2_local_normaliser.py` | before V2 | Deterministic extraction → per-REQ JSON + `_gaps_manifest.json` |
| `normalisation/extractors.py` | (lib) | Extraction library imported by the normaliser |
| `_paths.py` | (lib) | Path resolver — package-root-relative, `PIPELINE_ROOT`-overridable |
| `check_sdp_evidence.py` | pre-V2 review gate | Validates `output/SDP_EVIDENCE.json` against schema and semantic rules |
| `check_v2.py` | V2 Gate 2 (cross-reference) | Verifies guardrail linkage, hazard↔CHECK linkage, and traceability coverage; emits `FAIL:` lines |
| `pipeline_status.py` | orchestration status | Deterministic helper that reports `next_stage`, blockers, and stage completeness |
| `check_stage_contracts.py` | carry-forward contract gate | Validates shared stage output contract sections in `feedback/VALUE_CHAIN.md` |
| `check_clarification_state.py` | ambiguity lifecycle gate | Validates `feedback/CLARIFICATIONS.md` states (`open/routed/resolved/deferred`) |
| `check_control_coverage.py` | assurance coverage gate | Computes control-check evidence coverage from `output/*/checks.json` |
| `check_policy_drift.py` | governance drift gate | Validates required EMIS prompts contain the shared governance artifact references |
| `check_control_regression.py` | control regression gate | Detects control downgrades (`pass_criteria`/`test_scenarios`) and hazard orphaning in `output/*` |
| `preflight_v1f.py` | V2 → V1f gate (structural) | **"Verify V2 Complete"** — every REQ dir has all 7 JSON files, non-empty |
| `split_tasks.py` | after V1f | Splits `tasks_data.json` → `output/tasks/TASK-NNN.json` + `task_index.json` |
| `build_hazard_log_from_registry.py` | V1e (on demand) | Builds IF678 Hazard Log `.xlsx` from `HAZARD-REGISTRY.md` (one row per cause) using the bundled `.xlsm` template |
| `generate_pr1625_docx.py` | V1e IG (on demand) | Generates PR1625-style `.docx` from `output/PR1625_DPIA_DATA.json` |
| `generate_security_review_report.py` | V1e Security (on demand) | Generates CREST-ready markdown report from `output/SECURITY_ASSURANCE_DATA.json` |
| `ci_check_normaliser_outputs.py` | CI (optional) | Fails CI on `validation_error` / leftover `MISSING:` markers |

Other package files:

| Path | Purpose |
|---|---|
| `schemas/v2_output_schemas.json` | V2 output JSON schema contract (resolved via `config.yaml`) |
| `schemas/sdp_evidence_schema.json` | Project-level Secure Development Process evidence schema |
| `schemas/pr1625_dpia_schema.json` | Structured PR1625 DPIA JSON schema |
| `schemas/security_assurance_schema.json` | Structured security assurance JSON schema |
| `templates/if678-clinical-safety-hazard-log-increment.xlsm` | Layout template the hazard-log builder copies styles from |
| `requirements.txt` | Python deps: PyYAML, jsonschema, openpyxl, python-docx (+ optional sqlparse) |
| `config.yaml` | Package-root-relative paths (schema, agents, hazard_template) |

See [README.md](README.md) for the full agent↔script dependency map and what each generates.

---

## Stale files (left behind in `.github/agents/EMIS/`, not copied)

| File | Reason |
|---|---|
| `V1a_prototype_v7.agent.md.old.md` | Backup leftover |
| `V1c_design_nonemis_v1.agent.md` | non-EMIS variant misplaced in EMIS folder |
| `V1e_clinical_safety_v8.agent.md` | Superseded by v11 |
| `V1e_clinical_safety_v9.agent.md` | Superseded by v11 |
| `V1e_clinical_safety_v10.agent.md` | Superseded by v11 (Docman) |
| `V2_normalisation_v8.agent.md` | Superseded by v10; references dead `llm_augment.py` |
| `generate_hazard_csv.py` | Superseded by the Excel builder `build_hazard_log_from_registry.py` |

---

## Known issues

Verified by reading the agent prompts + scripts directly. Both items recorded at
baseline are now **resolved** (June 2026):

1. ✅ **`check_v2.py` (V2 cross-reference gate) authored.**
   `scripts/check_v2.py` now implements the gate `V2_normalisation.agent.md`
   references: it cross-references guardrail IDs ↔ `CS_Guardrails.json`, hazard
   `check_id`s ↔ `checks.json` (with `hazard_id` match), and CHECK ↔
   `traceability.json`, emitting `FAIL:` lines and a non-zero exit. It remains
   **distinct from `preflight_v1f.py`**: `check_v2.py` is V2's *cross-reference*
   (semantic) gate; `preflight_v1f.py` is the *structural-completeness* gate. The
   agent's Gate 2 now invokes the packaged script directly (no copy-to-output).
2. ✅ **`CS_Guardrails.json` producer restored.**
   `V2_normalisation` Phase 4.5 now instructs the agent to produce/refresh the
   project-wide `output/CS_Guardrails.json` register (preserve-if-unchanged, else
   rebuild from the union of cited guardrail IDs). This satisfies
   `preflight_v1f.py` (line 98) and `V1f_planning` input #11 — closing the v10
   regression where the v8 producer step had been dropped.

No open issues remain in the V2 → V1f handoff.

---

## Refactor backlog (after baseline)

1. ✅ Done — restored the `CS_Guardrails.json` producer step in `V2_normalisation`
   (Phase 4.5) so `preflight_v1f.py` and V1f have their required input.
2. ✅ Done — authored `scripts/check_v2.py`, the V2 cross-reference gate (smoke-
   tested: detects guardrail/hazard/traceability defects; degrades to WARN when a
   reference file is absent).
3. Add reviewer agents (`*_REVIEWER`) to the package if the review stage is
   brought into the master baseline.
4. Fix any inter-agent file references that still cite `_vN` suffixed filenames
   now that the package is suffix-free.
