# Pipeline Value Chain Contract

Every agent in this pipeline must follow this contract. The contract has three
parts: a **pre-start check**, a **carry-forward rule**, and a **fail-closed rule**.

No agent may proceed if its pre-start check fails.
No agent may complete without emitting a carry-forward block.
No agent may silently drop a control, constraint, gap, or requirement.

---

## Standard Pre-Start Check (apply verbatim to each agent)

```
⛔ PRE-START CHECK — before reasoning about any requirement

1. Confirm required inputs exist and are non-empty.
2. Confirm the previous stage's carry-forward block is present.
3. If any required input is missing: STOP. State what is missing. Do not proceed.
4. If previous carry-forward is absent: STOP. Ask the user to re-run the prior stage.
```

---

## Standard Carry-Forward Block (emit at end of each agent session)

Each agent must write the following block to `feedback/VALUE_CHAIN.md`
(append, never overwrite) at the end of its session:

```markdown
## {STAGE} — {DATE}

### Consumed from upstream
- {item_type}: {item_description} [source: {prior_stage}]
- ...

### Added by this stage
- {item_type}: {item_description} [guardrail/check_id if applicable]
- ...

### Must be preserved by next stage
- {item_type}: {item_description} — downstream: {next_stage}
- ...

### Gaps declared (must not be silently dropped)
- {gap_description} — status: open | deferred | resolved

### Edge cases covered
- {edge_case_description}

### Residual risks
- {residual_risk_description or none}
```

---

## Per-Stage Expected Inputs

| Stage | Required upstream inputs | Pre-start check |
|---|---|---|
| Pipeline 01 Requirements | manifest.md (user-provided) | manifest.md exists + non-empty |
| Pipeline 02 Prototype | Pipeline 01: REQ*.md files with Evaluation Function Spec + CHECKs | Each REQ has `## Evaluation Function Specification` and at least 1 CHECK |
| Pipeline 03 Architecture | Pipeline 02: prototype validation evidence or explicit skip | Prior stage carry-forward present in `feedback/VALUE_CHAIN.md` |
| Pipeline 04 Design | Pipeline 03: Each REQ has `## Architecture (Added by Pipeline 03)` with BDAT + ADRs | `## Architecture (Added by Pipeline 03)` present in every in-scope REQ |
| Pipeline 05 PxD | Pipeline 04: Each REQ has `## Design (Added by Pipeline 04)` with API contract + schema | `## Design (Added by Pipeline 04)` present in every in-scope REQ |
| Pipeline 06 Clinical Safety | Pipeline 05: Each REQ has `## PxD (Added by Pipeline 05)` | `## PxD (Added by Pipeline 05)` present in every in-scope REQ |
| Pipeline 07 Information Governance | Pipeline 06 (or 05 for non-clinical): prior stage sections present | Required upstream sections present + carry-forward block in VALUE_CHAIN.md |
| Pipeline 08 Security | Pipeline 03 security framing decisions present; prior V1e-equivalent sections present | Security framing answers present in Architecture section |
| Pipeline 09 Normalisation | `v2_local_normaliser` run has produced `_gaps_manifest.json` per REQ | `_gaps_manifest.json` exists for every REQ in scope |
| Pipeline 10 Planning | Preflight gate passed; all Pipeline 09 JSON files present and non-empty | `output/planning/PREFLIGHT_STATUS.json` status=passed; all per-REQ JSON non-empty |
| Coding Agent | TASK-NNN.json file loaded; `checks[]` non-empty; `pass_criteria` non-empty | Task file self-consistent; `files_to_read` ≤5 |

---

## Carry-Forward Expectations (what next stage must consume)

| Stage | Must carry forward to next stage |
|---|---|
| Pipeline 01 → Pipeline 02/03 | Requirement IDs, CHECKs, acceptance criteria, explicit gaps, hazard notes |
| Pipeline 03 → Pipeline 04 | ADRs, security framing answers, failure modes, platform decisions, trust boundaries |
| Pipeline 04 → Pipeline 05 | API contract signatures, DB schema, interfaces, state machines, traceability |
| Pipeline 05 → Pipeline 06/07/08 | User flows, UI constraints, exit states, accessibility rules, component specs |
| Pipeline 06/07/08 → Pipeline 09 | Hazard IDs, IG controls, security controls, attack-vector coverage, gap register |
| Pipeline 09 → Pipeline 10 | Enriched JSON (interfaces, components, observability, hazards, CHECKs) + CS_Guardrails.json |
| Pipeline 10 → Coding Agent | Task file with CHECKs, guardrails, file paths, pass criteria, verification command |
| Coding Agent → done | Code + tests that satisfy all CHECKs with binary evidence |

---

## Fail-Closed Rules (apply to every stage)

1. If a required input is missing: stop, state what is missing, do not proceed.
2. If a carry-forward item from the prior stage is absent: stop, ask user to re-run prior stage.
3. If a gap cannot be resolved in this session: emit it explicitly; do not silently drop it.
4. If a control has no CHECK: block completion.
5. If a requirement delta has no downstream target: flag it as untraced.
6. `check_value_chain.py` must exit 0 before V1f emits tasks.

---

## Automated Validator

```bash
python3 pipeline/scripts/check_value_chain.py --project /path/to/project
```

Checks:
- Every REQ has the mandatory heading sections from each completed stage.
- Every CHECK in the requirement has a `guardrail_id` or `hazard_id` provenance tag.
- Every V2 JSON file has `checks[]` non-empty and traceable.
- Every task in `tasks_data.json` maps to at least one upstream CHECK.
- No unresolved `MISSING:` markers in any output JSON.
- `feedback/VALUE_CHAIN.md` exists and has entries from all completed stages.

Exit 0 = all checks pass. Exit 1 = list of failures printed to stdout.
