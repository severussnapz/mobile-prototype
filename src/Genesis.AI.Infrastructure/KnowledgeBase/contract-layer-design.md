# Contract Layer — Design

**Status:** Partially implemented — artefact pattern adopted July 2026. Tool-layer backstop, TDD gate API enforcement, and guardrail suite remain.
**Plan reference:** Plan 4d, item 1 (Contract enforcement) and item 17 (Contract layer design session — no implementation before this design is signed off).
**Owner:** Idris Issa.
**Prerequisite for:** Plan 5 (TDD Agent). This design is the sign-off gate; implementation may proceed once approved.

> **Enforcement decided (§9a):** injection via the existing per-turn prompt rebuild, plus a locked-in tool-layer backstop on the contract. **TDD gate decided (§9b-i):** strict form — manifest pins REQ+ARCH provenance, gate blocks on drift, hard rule keyed off domain badges governs fast-path vs full re-run. **Tag vocabulary decided (§9b):** stable `tagId` identity (not path-based), renames surfaced as human-confirmed reviewable events. All verified against the codebase where applicable, not assumed. The SESSION-CLOSE re-injection gap found during this design is fixed and committed (first instance of the injection-contributor pattern).

---

## 1. Why this exists

The highest-risk failure in the pipeline after clinical safety is **layer drift** — where the frontend, backend, and generated code silently disagree because each was produced against a different understanding of the same contract. The model is a text predictor; without a frozen, shared reference it will produce something plausible at every layer that quietly disagrees with every other layer.

This failure mode is silent. It does not fail at design time. It fails in production, in a stakeholder demo, or — worst — in a clinical workflow. It has already occurred twice in a single session as the DTO mapping completeness failure: fields computed in a handler, present in the result, silently discarded at the HTTP layer, with a green build and 927 passing tests.

At EMIS scale (35M patients, 3,500 practices, DCB0129 obligations) a drift between the clinical safety artefact and the implemented behaviour is not a bug — it is a patient safety incident. The clinical safety artefact says the system does X; the code does Y; nobody caught it because each layer looked correct in isolation.

The contract layer removes the ambiguity that causes drift. It is the structural antidote to vibe-coding debt: a single, versioned, frozen source of truth that every downstream stage is bound to.

---

## 2. What the contract is

The contract is **four plain-text files**, produced at P04 (Design — API/DB):

| File | Purpose | Consumed by |
|---|---|---|
| `API-CONTRACT.yaml` | OpenAPI specification — every endpoint, request/response shape | NSwag (TypeScript generation), P11 code generation, TDD agent |
| `DB-SCHEMA.sql` | Flyway migration — tables, columns, constraints, indexes | P11 code generation, TDD agent |
| `DATA-MODELS.md` | Human-readable entity/field summary + the **traceability section** (see §6) | P05–P08 review stages, humans |
| `ERROR-CATALOGUE.md` | Every error condition, `userMessage`, HTTP status, endpoint | Frontend (user messages), P11 |

**Plain text, not a bundle/zip.** Each file is independently readable, diffable, and greppable. Git shows exactly what changed between versions line by line. No extraction, no binary blob, no faff.

Each file is a normal Genesis artefact under `.genesis/design/`:

```
.genesis/design/
  API-CONTRACT.yaml
  DB-SCHEMA.sql
  DATA-MODELS.md
  ERROR-CATALOGUE.md
  CONTRACT-MANIFEST.md          ← the manifest (see §3)
```

---

## 3. Versioning and the manifest

### Reuse the existing versioning mechanism

The artefact model already versions per `(ProjectId, FilePath)`. `GetArtefactVersionsQuery(ProjectId, FilePath)` returns all versions of one artefact; the latest version for a filePath is "the artefact." Each contract file is versioned by this existing mechanism — exactly like `REQ-001.md` is today. **No schema change, no new `v1/`/`v2/` directory hierarchy.**

### Built pattern: CONTRACT-MANIFEST.md as the coherence anchor

PROVEN (implemented in prompt/runtime contract, not DB aggregate): contract coherence is represented by a versioned artefact, `design/CONTRACT-MANIFEST.md`, following a six-section structure:

1. `Status Header` (includes provenance comments)
2. `Pinned File Versions`
3. `Requirement Ledger`
4. `Shared Element Index` (Endpoints, Tables, Data Models, Error Codes)
5. `Reuse Log`
6. `TDD Gate (Plan 5)`

PROVEN comment format in use:

```html
<!-- contract-manifest-version: {N} -->
<!-- req-provenance: {filePath}@v{version},... -->
<!-- arch-provenance: architecture/ARCH.md@v{version} -->
```

This replaces the earlier CONTRACT.md-style "pin file" description with the concrete artefact pattern aligned to the existing HAZARD-REGISTRY style: plain text, versioned per file path, and directly consumable in prompt context.

ASSUMED until tool backstop lands: downstream contract reads are expected to stay aligned to the pinned set via prompt injection and warnings; hard enforcement at tool resolution is specified but not yet implemented (see §9a).

---

## 4. Breaking changes reuse the CHANGE-record pattern

A contract version bump is **not new machinery**. It reuses the existing CHANGE-record + domain-badge pattern already built for requirement changes:

- A contract change produces a new manifest version plus a `CHANGE-{id}.md` record.
- The CHANGE record carries **domain impact badges**: CS (Clinical Safety), IG (Information Governance), SEC (Security).
- A badge marks the corresponding downstream stage as requiring re-review.
- The same propagation that flags P06 when a REQ changes now flags P06 when the contract changes.

---

## 5. Resumption and staleness

PROVEN (implemented): staleness is checked from manifest provenance comments, not from a DB contract aggregate.

`ContractManifestStalenessChecker`:

- Parses HTML provenance comments in `CONTRACT-MANIFEST.md`:
  - `contract-manifest-version`
  - `req-provenance`
  - `arch-provenance`
- Resolves each pinned entry against current approved artefact versions via `IArtefactRepository.GetByProjectAndFilePathAsync(...)`.
- Emits targeted warnings when a pinned artefact is missing or drifted.

PROVEN (implemented): `ConversationStreamController` runs this check per turn and appends warnings into the mutable prompt part so drift is visible in runtime context, not just documented.

ASSUMED until tool backstop lands: prompt-level warnings reduce silent drift risk, but do not yet force tool resolution to pinned versions if a later tool call asks for a contract artefact explicitly (see §9a NOT YET BUILT).

---

## 6. Tagging and badge governance

The badge model is only as trustworthy as whatever assigns the tags. If a safety-relevant change is not badged, the stage is never flagged and the drift is invisible **and** blessed by a gate — worse than no badge. Governance of the tagging is therefore the crux.

### The chain of trust

1. **Tags are owned by the domain role-holder** — the CSO owns CS tags, the IG owner owns IG tags, the security reviewer owns SEC tags. These are the roles already captured in the P00 form.
2. **Tags are computed, not asserted.** Tags trace design elements (schema columns, endpoints) to upstream anchors (HAZ-IDs from P01 for CS; DPIA risk refs for IG; threat/control refs for SEC). A deterministic check fires the badge when a contract diff touches a tagged element.
3. **The agent proposes; the rule cross-checks.** The agent's self-classification is compared against the deterministic result. Agent says "no badge" but rule says "badge" → the rule wins and the discrepancy is logged (an attempt to under-badge is signal). Agent over-badges → badge applies anyway (fail safe), noted for tuning.
4. **The human adjudicates only genuinely new surface**, through the CODEOWNERS PR gate that already governs the regulated prompts.

### P04 drafts, P06/P07/P08 ratify

The person qualified to judge domain relevance does not arrive until the specialist stage. So tagging is split:

- **At P04 the agent drafts tags mechanically** by tracing elements to existing upstream anchors. Tags are marked **draft / unratified**. No clinical/IG/security judgement happens here — only traceability.
- **At P06/P07/P08 the role-holder ratifies** as a **by-product of the assessment they already do.** When the CSO concludes "the NHS number check is the safety-critical control here," they have — in that act — confirmed `nhs_number` and the patient-match step are CS-relevant. The tag is the structured echo of a sentence they would write anyway.
- **Ratification is a hard gate**: the stage cannot close with tags left in draft (same pattern as P01 blocking phase advance on unanswered mandatory questions).

**Review beats add.** Machine-drafting at P04 guarantees the role-holder is always reviewing a populated list, never a blank page where a missing tag looks identical to a deliberate "none." Omission is invisible; a bad entry is visible. This is the safer failure mode, and it is the reason to draft at P04 rather than tag from scratch at P06.

### The interim window (P04 → ratification)

Between P04 and ratification the contract carries only draft tags. This is currently safe because: (a) draft tags err toward over-tagging (mechanical tracing is conservative), so downstream sees more flags than necessary, not fewer; (b) nothing irreversible happens in the window — P05 is design, P11 code-gen is gated behind P06 anyway; (c) ratification is itself a contract change with a domain badge, so anything approved against draft-tag versions is retroactively flagged once ratified.

**This safety is by current arrangement, not by design.** Point (b) — the interim window being harmless — rests entirely on the pipeline ordering that P11 code generation sits behind P06 ratification. That is a dependency, not a guarantee. If the pipeline is ever reordered, or a downstream stage is allowed to consume the contract in the interim window for something that *does* commit irreversibly, this safety argument evaporates silently. Any change to the P11-behind-P06 ordering must trigger a re-examination of the interim-window safety.

### Corrections ride the existing feedback loop

When a role-holder removes a false positive or adds a missed tag, that correction is captured by the **existing feedback mechanism** (the same GAP/CLARIFICATION/CONTRADICTION-style human-over-agent correction the pipeline already records). No new store, no bespoke training pipeline. Accumulated corrections are the corpus for tuning the P04 tagging skill over time — the draft improves, the review gets lighter, and safety never moves because the floor is the ratification gate, not the accuracy of the draft.

### Three domains, independent

The pattern applies identically across all three regulated stages:

| Stage | Role-holder | Assessment (by-product) | Tag | Anchor |
|---|---|---|---|---|
| P06 Clinical Safety | CSO | DCB0129 hazard log | CS | HAZ-ID |
| P07 Information Governance | IG owner | DPIA | IG | DPIA risk ref |
| P08 Security | Security reviewer | Threat model | SEC | Threat/control ref |

The three tag domains are **independent**. A change carrying only a SEC badge pulls the security reviewer back into P08 and leaves the CSO and IG owner untouched. Each domain's tags trigger only that domain's re-review.

**Known asymmetry:** clinical safety has the richest anchor chain because HAZ-IDs already exist as first-class things from P01. IG and SEC anchors at P01 are lighter (P01 treats CS/IG/SEC as lightweight routing anchors, deferring deep elicitation to the specialist stages). So the P04 mechanical draft will be strongest for CS and thinner for IG/SEC — the IG/SEC tagging skills will lean more on the role-holder to *add* at ratification and will need more correction-driven tuning early on. Not a flaw (the ratification gate holds the floor regardless), but a resourcing fact.

### Where tags live

Tags live in a **structured traceability section inside `DATA-MODELS.md`** — a table mapping element → domain (CS/IG/SEC) → anchor (HAZ-ID etc.) → draft/ratified state. **Not** inline annotations in the schema/API files.

The reason is the user experience (§7): Genesis must be able to *query* the tags to render the role-holder's review worklist ("show me every CS-tagged element"). A structured section is a direct read; inline annotations scattered as comments across two files would force fragile parsing and let the tag drift from any derived view. Keeping the schema and API files clean also means NSwag and other tooling consume them without tripping over safety annotations.

---

## 7. User experience

The BA and the role-holders never see raw contract files. They see what Genesis renders.

**BA at P04:** the tagging happens underneath. The most the BA sees is a quiet indicator — "N elements flagged for clinical safety review" — and, if they open the design artefact, a subtle read-only marker on flagged fields. They do not manage or edit tags.

**Role-holder at P06/P07/P08:** sees the tagging as a **review worklist**, rendered from the traceability section — element, linked anchor, and two actions (confirm, or remove-with-reason), plus a way to add a missed element. It feels like part of the hazard log / DPIA / threat model they already work in, not like editing a file. Their confirmations and removals write back to the traceability section via the feedback loop, flipping draft → ratified.

**What the role-holder actually does (plain):** their normal assessment, plus a review of a pre-populated list rather than authoring one from scratch. The tag is a by-product of the judgement they are already making. Confirming the tag now is what arms the tripwire that pulls them back later if the contract changes in their domain — they are setting the alarm on the things that, in their professional judgement, need watching. (The workload of this review is not yet measured — it is proportionate to the amount of tagged surface, which is proportionate to genuine novelty in the design. This is a resourcing fact to put in front of the role-holders, not a fixed cost to claim.)

**Anyone later:** the traceability section is the audit record — what was flagged, by machine or human, confirmed by whom, when.

---

## 8. Settled decisions (summary)

1. Contract = four plain-text files (`API-CONTRACT.yaml`, `DB-SCHEMA.sql`, `DATA-MODELS.md`, `ERROR-CATALOGUE.md`) under `.genesis/design/`.
2. Versioned by the existing per-`(ProjectId, FilePath)` mechanism — no schema change, no new folder hierarchy.
3. A `CONTRACT-MANIFEST.md` manifest pins a coherent set of file versions; downstream stages pin the manifest version (one number).
4. Breaking changes reuse the existing CHANGE-record + domain-badge pattern.
5. Resumption uses a pin (stage records approved-against manifest version) + a staleness check on resume; badge decides whether re-review is forced.
6. Tagging: P04 drafts mechanically from upstream anchors (marked draft); P06/P07/P08 role-holders ratify as a by-product of their existing assessment; ratification is a hard gate.
7. Tag corrections ride the existing feedback loop; accumulated corrections tune the P04 tagging skill; the ratification gate holds the safety floor regardless of draft accuracy.
8. Tags live in a structured traceability section in `DATA-MODELS.md`, not inline — because Genesis renders the role-holder worklist from it.
9. Three independent tag domains (CS/IG/SEC), each owned by its P00 role-holder, each triggering only its own re-review.

---

## 9a. Enforcement

### DONE (PROVEN)

1. `ContractManifestContextBuilder` injects `design/CONTRACT-MANIFEST.md` content into prompt context for consuming stages:
  - Design
  - ClinicalSafety
  - InformationGovernance
  - Security
2. `ContractManifestStalenessChecker` parses manifest provenance comments and emits targeted staleness warnings.
3. `ConversationStreamController` appends those warnings into the mutable prompt part per turn.

### NOT YET BUILT (ASSUMED DESIGN INTENT)

1. Tool-layer backstop: when the agent calls `get_artefact` for contract files, enforce pinned-version resolution rather than latest.

Current enforcement is prompt-layer plus warning-layer. Hard pin enforcement at tool resolution remains outstanding.

---

## 9b. Tag vocabulary — DECIDED

The traceability section location is settled (§6: structured section in `DATA-MODELS.md`). This fixes the shape of a single tag entry.

### The entry

Each tag entry has these fields:

| Field | Purpose | Mutable? |
|---|---|---|
| `tagId` | Stable identity, assigned once at first tagging. Project-scoped sequential (`TAG-0001`…), same convention as HAZ-IDs. Never reused, never reassigned. | Immutable |
| `element` | Current human-meaningful path into the contract (e.g. `DB-SCHEMA.sql#patients.nhs_number`, `API-CONTRACT.yaml#/paths/patient-match/post`). **Descriptive, not identity** — used by the diff-matcher to detect when a change touches the element; can change on rename. | Mutable (a change is a reviewable event) |
| `domain` | CS / IG / SEC. Drives badge routing and re-review. | Set at tagging |
| `anchor` | Upstream reference justifying the tag: HAZ-ID (CS), DPIA risk ref (IG), threat/control ref (SEC). | Set at tagging |
| `state` | `draft` / `ratified`. | draft → ratified |
| `ratifiedBy` | Role-holder identity (role, resolved to person via RBAC). Null while draft. | Set at ratification |
| `ratifiedAt` | Timestamp. Null while draft. | Set at ratification |

### Why stable identity (not path-based identity)

The identity is `tagId`, **not** the element path. This was decided against the cheaper path-only option because path-based identity is **brittle**: on a rename or refactor the path changes and the tag silently detaches from the thing it was tracking. In a regulated clinical system, a silently-broken safety-relevant audit link is the specific catastrophe this layer exists to prevent — and it is *untraceable a year later*, after many refactors, when someone tries to reconstruct why a tag stopped tracking a field. The audit trail would have a hole with no way to prove the tag ever covered its element.

The severity of that failure — not its probability — drives the decision. A rename may be rare, but the cost when it happens is a broken safety-audit link, which is never acceptable. You do not gamble on the frequency of a low-probability, high-severity event when the mitigation (a stable ID) is cheap relative to the blast radius. (Note this is the same severity-over-probability logic as the TDD gate, §9b-i — invest in the durable form now, because retrofitting after the fact is the mass-correction trap.)

### How renames stay safe

With stable `tagId`, a rename becomes a **visible, reviewable event**, never a silent detach. When P04 re-runs and produces a new contract version, the tagging pass maps existing `tagId`s onto the new contract:

- Path unchanged → tag carries forward silently.
- Path changed, successor identifiable → surfaced as a "confirm this rename" review item (`TAG-0031` moved from A to B). The role-holder confirms, same as ratifying the original tag.
- Successor not identifiable → surfaced as "`TAG-0031`'s element has vanished; re-locate or retire it."

This reuses the diff machinery that already exists for staleness — not a new subsystem.

**Honest limitation:** stable IDs make the *identity* durable, but the rename-*mapping* heuristic (guessing A became B) can be wrong. The mitigation is that the guess is **always surfaced for human confirmation, never applied silently** — the role-holder ratifies a rename the same way they ratify an original tag. So the heuristic can be wrong without being dangerous. What stable IDs buy is not a perfect rename-tracker; it is the guarantee that a rename can *never silently break the link* — worst case is a flagged re-confirmation, not a hole in the audit trail.

---

## 9b-i. TDD gate

### DONE (PROVEN)

The TDD gate definition now lives in `CONTRACT-MANIFEST.md` section 6 as agent-readable text:

- Gate open = `YES` only when all REQ rows are `COMPLETE` and provenance is current.
- Gate open = `NO` when any REQ remains pending or provenance drifts.

### NOT YET BUILT (ASSUMED DESIGN INTENT)

API/runtime enforcement of this gate is not yet implemented. Today, the gate is documented and injected for agent guidance, but not yet hard-blocked by backend policy checks.

---

## 9c. Guardrail suite status

### DONE (PROVEN)

1. Seam type 4 (tool registration → wiring): `ToolCallWiringTests` coverage exists.
2. `ContractManifestContextBuilder` unit tests exist (write/read context path to prompt injection input).
3. `ContractManifestStalenessChecker` unit tests exist (provenance parsing and warning behaviour).

### NOT YET BUILT (ASSUMED DESIGN INTENT)

1. Seam type 1: result → HTTP body, class-level completeness tests.
2. Seam type 2: command → route existence checks.
3. Seam type 3 (strong form for contract manifest): write → resume → assert in rebuilt prompt, as an integration seam.
4. Seam type 5: pin → resolution end-to-end tests proving pinned-version retrieval, not latest.

---

## 10. Sequencing note

Implementation status and remaining order:

1. ✅ Item 1 — `CONTRACT-MANIFEST.md` artefact pattern (replaces DB aggregate as runtime contract source).
2. ✅ Item 2 — pin + staleness check (`ContractManifestStalenessChecker`).
3. ✅ Item 3 — contract injection (`ContractManifestContextBuilder` + P04/P06/P07/P08 prompt consumption blocks).
4. ⬜ Item 3 (partial outstanding) — tool-layer backstop for pinned-resolution at tool-call time.
5. ⬜ Item 4 — tagging implementation deferred (Decision E).
6. ⬜ Item 5 — TDD gate API enforcement.
7. ⬜ Item 6 — guardrail seam suite completion (types 1, 2, 3, 5).

PROVEN vs ASSUMED: items marked ✅ are implemented in code/prompt assets; ⬜ items are intended architecture and remain open work.

## 11. Architectural decision: DB aggregate -> artefact pattern

Decision (July 2026): move runtime contract coherence from the proposed DB-first aggregate path to the artefact-first path (`CONTRACT-MANIFEST.md`), following the existing versioned artefact operating model.

Rationale:

1. The DB aggregate proposal (`ContractManifest` + `ContractManifestPin`) reliably represented version pins (§2), but did not carry the full anti-drift operating surface used by the agent flow:
  - requirement ledger progression,
  - shared element index,
  - reuse log,
  - TDD gate declaration.
2. The artefact pattern keeps those controls in one readable, versioned, prompt-consumable file and matches established stage patterns.

What happens to the DB aggregate work:

1. `ContractManifest` / `ContractManifestPin` entities and migration V25 remain committed.
2. They are kept dormant for now and are not the active runtime source of truth.
3. Planned activation point: when API-level Plan 5 gate enforcement is implemented, parse `CONTRACT-MANIFEST.md` section 2 and materialise the existing aggregate as the machine-checkable pin record.

ASSUMED until that activation work lands: dormant aggregate persistence remains intentionally unused by runtime orchestration.
