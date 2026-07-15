# Artefact Scope Restructure — Solution Design

**Purpose:** De-bloat the REQ file. Each pipeline stage's full output moves into a dedicated artefact — per-requirement or per-project — and the REQ becomes a lightweight index of references into those artefacts. Introduces `TEST-{id}.md` as the per-requirement test registry consumed by the Two-Agent TDD flow. For sign-off — no implementation before sign-off. Lands with Plan 5.

**Companion designs:** `plan-4e-flow-spec.md` (flow model, AC stable IDs), `contract-layer-design.md` (manifest pinning, staleness machinery). This design reuses both.

---

## Why this exists

A REQ file today accumulates every stage's full output additively: P03 architecture (12 sub-sections), P04 design, P05 PxD, P06 clinical-safety summary, P07 IG, P08 security, plus handoff notes. Across twenty requirements this produces multi-thousand-line files. Three consequences follow, and all three worsen as the pipeline scales:

1. **Retrieval degrades.** A 3,000-line REQ file chunked for pgvector returns diffuse, low-precision hits. The knowledge layer's recall drops precisely where it matters most — the regulated stages buried deep in the file.
2. **Context pressure on downstream agents.** Agent B (Plan 5) working on one requirement should load that requirement's context, not the whole project's architecture prose. A bloated REQ forces the whole file into context or an imprecise slice of it.
3. **Change blast radius is opaque.** When a requirement changes, it is not clear which stage sections in the monolithic file are affected — the staleness signal is file-level, not section-level.

The hazard registry already solved this for P06: detailed hazard cards live in `HAZARD-REGISTRY.md`; the REQ gets HAZ-ID references only. This design generalises that proven pattern to every stage.

---

## Load-bearing decisions (everything else stands on these)

**1. The REQ file is an index, not a container.** After this change, `REQ-{id}.md` holds only what P01 owns — requirements, acceptance criteria (with stable AC IDs), the Evaluation Function Specification, compliance anchors — plus a References section pointing into every downstream artefact. No downstream stage writes its full output into the REQ. It writes a one-paragraph summary and a reference. This is the completeness test on the word "index": if a stage's content can only be found by reading the REQ, the extraction is incomplete.

**2. Scope is decided per stage by who ratifies it, not by convenience.** A stage's output is per-project when a single role ratifies it holistically across all requirements (the CSO signs one hazard registry; the IG owner signs one DPIA; the security reviewer signs one control set; architecture and design patterns are project-wide decisions all requirements inherit). It is per-requirement when it is scoped to one requirement's behaviour and consumed by an agent working on that requirement alone (the requirement itself, its flow, its change history, its tests). This rule is the design's backbone — it is not a filing preference.

**3. Traceability is bidirectional and explicit.** The REQ references the project artefact; the project artefact references back the requirement IDs it serves. `HAZARD-REGISTRY.md` HAZ-ID entries carry `REQ-{id}` references; `ARCH.md` sections carry the `REQ-{id}`s they serve; every `SECURITY-REGISTRY.md` control names the requirements it protects. With per-requirement files the link was implicit in the filename; with project-level files it must be written, and its absence is a P09 normalisation failure, not a silent gap.

**4. Staleness is scoped by requirement reference, not by file.** A project-level artefact serves many requirements. When `REQ-042` changes, only the sections of `ARCH.md`, `DESIGN.md`, `IG.md` etc. that reference `REQ-042` go stale — not the whole file. This requires project-level artefacts to be internally sectioned by requirement reference so the staleness machinery can resolve "which sections does this requirement change touch?" deterministically. Without this, every requirement change marks every project artefact wholly stale and the model collapses into re-review churn. This is the one genuinely new piece of machinery the design needs — everything else is reuse.

**5. `TEST-{id}.md` is per-requirement, generated from approved artefacts only, never from memory.** Agent A assembles the test registry for a requirement by reading the approved upstream artefacts via `get_artefact`. It generates a test section only for a stage whose artefact is approved. If P06 has not run, the clinical-safety section is empty — the agent does not invent hazard tests. The approved artefact is the gate; there is no hallucination surface.

---

## The target artefact structure

| Stage | Full content lives in | Scope | REQ file gets |
|---|---|---|---|
| P01 Requirements | `REQ-{id}.md` | Per requirement | Core — the requirement itself |
| P01 Flow (Plan 4e) | `FLOW-{id}.md` | Per requirement | Flow reference |
| P01 Change history | `CHANGE-{id}.md` | Per requirement | — |
| P03 Architecture | `ARCH.md` | **Per project** | Summary + ADR/section references |
| P04 Design | `DESIGN.md` + `API-CONTRACT.yaml`, `DB-SCHEMA.sql`, `DATA-MODELS.md`, `ERROR-CATALOGUE.md` | **Per project** | Summary + contract references |
| P05 PxD | `PXD.md` | **Per project** | Component + pattern references |
| P06 Clinical Safety | `HAZARD-REGISTRY.md` (+ `.xlsx` export) | **Per project** | HAZ-ID references |
| P07 IG | `IG.md` / `DPIA.md` | **Per project** | IG control references |
| P08 Security | `SECURITY-REGISTRY.md` | **Per project** | Security control references |
| P11 TDD | `TEST-{id}.md` | Per requirement | Test count + coverage reference |
| P00 Project init | `PROJECT.md` | Per project | Read from project context |

**Per-requirement artefacts** — the only files carrying a `{id}` suffix: `REQ-{id}.md`, `FLOW-{id}.md`, `CHANGE-{id}.md`, `TEST-{id}.md`. These are what an agent loads for a single task.

**Per-project artefacts** — one file each, ratified once by the owning role, referenced by many requirements: `ARCH.md`, `DESIGN.md`, `PXD.md`, `HAZARD-REGISTRY.md`, `IG.md`/`DPIA.md`, `SECURITY-REGISTRY.md`, `PROJECT.md`.

### Feature repo layout (after)

```
{feature-repo}/
  .genesis/
    requirements/         REQ-{id}.md, CHANGE-{id}.md, FLOW-{id}.md, TEST-{id}.md
    architecture/         ARCH.md
    design/               DESIGN.md, API-CONTRACT.yaml, DB-SCHEMA.sql,
                          DATA-MODELS.md, ERROR-CATALOGUE.md, CONTRACT.md
    pxd/                  PXD.md
    clinical-safety/      HAZARD-REGISTRY.md, HAZARD-REGISTRY.xlsx
    ig/                   IG.md, DPIA.md
    security/             SECURITY-REGISTRY.md
    prototype/            index.html
    session-close/        SESSION-CLOSE-P01.md … SESSION-CLOSE-P08.md
    review/               REVIEW-{id}.md
    project/              PROJECT.md
```

Change from today: `ARCH-{id}.md` → project-level `ARCH.md`; `IG-{id}.md` → `IG.md`; `SEC-{id}.md` → `SECURITY-REGISTRY.md`; new `pxd/PXD.md`; new `requirements/TEST-{id}.md`; `DCB0129-{id}.*` consolidated into the project-level `HAZARD-REGISTRY.*` (the per-area registry the CSO already owns).

---

## The REQ file as index

After the change, `REQ-{id}.md` contains only P01-owned content plus references:

- `## Requirements` — the requirement statements
- `## Acceptance Criteria` — ACs, each with a stable `AC-{req_id}-{seq}` ID (Plan 4e Phase 1 dependency)
- `## ✨ Evaluation Function Specification` — the CHECKs, the deterministic pass/fail criteria
- `## Compliance Anchors` — `@CS` / `@IG` / `@SEC` routing flags
- `## References` — the index block:

```
## References
- Flow:            FLOW-{id}.md
- Architecture:    ARCH.md § {req-id}                 (summary: one paragraph)
- Design:          DESIGN.md § {req-id}, API-CONTRACT.yaml#{operationId}
- PxD:             PXD.md § {req-id}
- Clinical Safety: HAZARD-REGISTRY.md → HAZ-031, HAZ-044   (summary: one paragraph)
- IG:              IG.md § {req-id}
- Security:        SECURITY-REGISTRY.md → SEC-012, SEC-019
- Tests:           TEST-{id}.md  (N functional, N NFR, N safety, N security, N IG, N eval)
```

Each downstream stage, on approval, writes its full content to its own artefact and writes a one-paragraph summary + reference into the REQ's References block. The summary is deliberately thin: enough for a human scanning the REQ to know the shape of the decision, never enough to duplicate the artefact.

---

## `TEST-{id}.md` schema (Plan 5)

One test registry per requirement. Agent A writes it; Agent B reads it and cannot modify it. Six sections, each sourced from a specific approved artefact from a specific stage:

| Section | Source artefact | Stage(s) |
|---|---|---|
| Functional | ACs (`AC-{req_id}-{seq}`) + `FLOW-{id}.md` paths | P01 + P04e |
| Non-functional | REQ NFRs + `ARCH.md` § req + `DESIGN.md` § req + `API-CONTRACT.yaml` | P01 + P03 + P04 |
| Clinical safety | `HAZARD-REGISTRY.md` HAZ-IDs referenced by this req | P06 |
| Security | `SECURITY-REGISTRY.md` controls referenced by this req | P08 |
| IG | `IG.md` / `DPIA.md` controls referenced by this req | P07 |
| Evaluation | `## ✨ Evaluation Function Specification` in REQ | P01 |

Rules:

- **Every test references its source.** A test carries the `AC-{req_id}-{seq}`, `HAZ-ID`, `SEC-ID`, `IG-control`, flow path, or CHECK it derives from. A test with no traceable source is a P09/Review-Agent failure — it means the agent invented behaviour.
- **NFR tests span three stages.** Performance targets from P01; API response-time / throughput / rate-limit contracts from P03 and `API-CONTRACT.yaml`; DB query and connection contracts from P04. Agent A reads all three and emits one test per contracted value — no duplication, each traced to its source. NFR tests cannot be generated before P03 and P04 are approved; the agent reads contracted values, it does not guess targets.
- **Functional tests come from two sources.** ACs give the assertions; flow paths (when a `FLOW-{id}.md` exists) give the branch/loop/termination coverage matrix. Absent a flow, functional tests degrade to AC-derived only — consistent with Plan 4e decision 4 (flow enriches, never gates).
- **Empty sections are correct, not incomplete.** A stage that has not run yet yields an empty section. Agent A never fills an empty section from memory.

---

## Change management and updates (baked in — reuses existing machinery)

No new change mechanism. The existing staleness and change-propagation machinery covers the whole restructure, with one extension (decision 4).

**Requirement changes (`propose_requirement_change`, Plan 3d):**
- An approved AC change updates `REQ-{id}.md` and, via the requirement-reference staleness resolver, marks stale exactly the sections of `ARCH.md`, `DESIGN.md`, `PXD.md`, `IG.md`, `SECURITY-REGISTRY.md` that reference this requirement, plus `HAZARD-REGISTRY.md` entries referencing it, plus `TEST-{id}.md` wholesale.
- The owning role re-reviews only the flagged sections; the rest of the project artefact is untouched.
- `TEST-{id}.md` stale → Agent A re-drafts from the updated sources → human approves → the `TASK-NNN-CODE.can_start = false` gate holds Agent B until the new tests are approved.

**Flow changes (Plan 4e):** a flow condition edit proposes an AC change back through Plan 3d; the AC change then propagates as above. Flow structural changes mark `TEST-{id}.md`'s functional section stale.

**The staleness signal is already implemented** — the per-turn prompt rebuild's `stalenessNotice` comparison used across every artefact applies here unchanged; the only addition is scoping it to referenced sections within a project-level file rather than the whole file.

---

## Downstream consumption

- **Agent A (test author, Plan 5)** reads the approved upstream artefacts for one requirement via `get_artefact` and writes `TEST-{id}.md`. It never sees Agent B's implementation.
- **Agent B (implementer, Plan 5)** loads `REQ-{id}.md` (thin index) + `TEST-{id}.md` + the referenced project-artefact sections it needs — never the whole project's architecture prose. This is the context-pressure win: precise, requirement-scoped context.
- **The knowledge layer** indexes small, structure-clean files. Recall improves because each artefact is single-concern and the structure-aware chunker's heading paths are meaningful (`HAZARD-REGISTRY.md > HAZ-031 > Mitigation`, not `REQ-042 > … > buried clinical section`).
- **P09 Normalisation** shifts from "verify every mandatory section is present in the REQ" to "verify every REQ reference resolves to an existing artefact section, and every project-artefact section carries its requirement references." Referential integrity replaces section-presence.

---

## Disciplines

- **The REQ is a thin index** — a stage that writes its full body into the REQ has failed the extraction. Summary + reference only.
- **Every project artefact section carries its requirement references** — the back-link is written, not implied; its absence is a normalisation failure.
- **Tests trace to sources** — no test without an `AC-`/`HAZ-`/`SEC-`/`IG-`/CHECK/flow-path anchor.
- **Empty is correct** — Agent A emits nothing for an un-run stage; it never fabricates.
- **Staleness is section-scoped** — a requirement change touches only referencing sections, never whole project files.

---

## Deferred framing (named, not accidental)

**Concurrent-write contention on project-level files is a Plan 6 concern, not a requirements-pipeline concern.** The requirements pipeline is sequential and human-gated at every stage approval — one requirement advances through P01→P08 at a time, so two agents never write `ARCH.md` simultaneously. The contention scenario only arises when the Code Swarm (Plan 6) runs multiple requirements in parallel, and by then there is real session data to choose the right mechanism (file-section locking, or per-project-artefact write serialisation). Building that machinery now would be speculative. Named here so it is a deliberate deferral, not an oversight.

**Project-level files themselves can grow.** `ARCH.md` accumulates architecture across all requirements; over a large migration it could bloat one level up. Mitigated by the structure-aware chunker (it handles large well-sectioned files) and by the requirement-reference sectioning (decision 4) that keeps it navigable. If it becomes a live problem, the refactor is to split a project artefact by capability grouping — a real-world-triggered change, not a day-one requirement.

**Refactor-once-in-production is the accepted posture.** The advantage of fewer files and single-owner ratification outweighs the theoretical risks above. Where a risk is a scale problem not yet hit, it is deferred to be informed by production evidence rather than pre-engineered — consistent with proven-over-new and shortest-working-diff.

---

## Dependencies

**Proven / reused (no new build):**
- Per-requirement conversations + `requirement_id` FK (Plan 1).
- `propose_requirement_change` + AC-insertion + domain-badge change machinery (Plan 3d).
- Immutable S3 versioning; `.genesis/` artefact push (Plan 4c).
- The per-turn `stalenessNotice` injection (Plan 1 / contract layer).
- The `HAZARD-REGISTRY.md` per-project registry pattern (Plan 4c D6) — generalised here.
- `get_artefact` cross-stage read (Mission 30 Action 10).

**Requires (upstream in the plan):**
- **AC stable IDs** — `AC-{req_id}-{seq}`, built in Plan 4e Phase 1. Prerequisite for functional-test AC references and flow `ac_ref`. Not shortcut.

**New build (Plan 5):**
- Per-stage prompt changes P03–P08: write full body to the dedicated artefact; write summary + reference into the REQ References block; write the requirement back-reference into the artefact section.
- Requirement-reference staleness resolver (decision 4) — resolves a changed `REQ-{id}` to the referencing sections across project-level artefacts.
- `TEST-{id}.md` schema + Agent A generation + the six sourced sections.
- P09 normalisation rewrite: reference-resolution + back-reference-presence checks replace section-presence checks.

**No migration — the system handles both shapes.** New projects use the new structure from day one. Existing projects keep their current REQ files unchanged. The pipeline is structure-tolerant: a stage resolves its content whether it sits as a section inside the REQ (old shape) or as a reference to a dedicated artefact (new shape). Nothing is rewritten in flight. Old projects age out naturally as the migration programme completes them. This is a deliberate choice over a one-off bulk migration — a bulk rewrite of live regulated artefacts carries risk that the mixed-read approach avoids entirely, and the structure-tolerance is a small, contained addition to each stage's read path.

---

## Status, owner, positioning

**Status:** 📋 IN DESIGN.
**Owner:** Idris.
**Positioning:** Plan 5 design decision. Lands with Plan 5 (Two-Agent TDD) because `TEST-{id}.md` is Plan 5 and Agent A depends on the clean, single-concern artefact structure. Not a Plan 4d item and not urgent before the production flag flip — the REQ bloat is not yet a live production problem because the pipeline is not yet running at volume. Depends on AC stable IDs (Plan 4e Phase 1) being in place first.
**Effort (indicative, pre-build — not committed):** ~1–1.5 weeks for the staleness resolver + P09 rewrite + prompt changes; the `TEST-{id}.md` generation is scoped within Plan 5's Agent A work.

**Suggested phasing:**
1. Requirement-reference staleness resolver + P09 reference-integrity checks (backend + normalisation).
2. Per-stage prompt changes P03–P08 (summary + reference out, back-reference in); structure-tolerant read path so old and new REQ shapes both resolve.
3. `TEST-{id}.md` schema + Agent A generation, with Plan 5.
