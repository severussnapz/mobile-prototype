# Contract Layer — Design

**Status:** Partial design. Conceptual model settled; enforcement mechanism decided (§9a); TDD gate, guardrail set, and tag vocabulary open.
**Plan reference:** Plan 4d, item 1 (Contract enforcement) and item 17 (Contract layer design session — no implementation before this design is signed off).
**Owner:** Idris Issa.
**Prerequisite for:** Plan 5 (TDD Agent). No TDD work begins until the remaining open questions (§9b) are resolved.

> **Enforcement is decided (§9a): injection via the existing per-turn prompt rebuild, with an optional cheap tool-layer backstop on the contract specifically.** This was verified against the codebase, not assumed — the per-turn rebuild with `stalenessNotice`/`handoverBlock` contributors already exists in `ConversationStreamController.cs`. A separate finding surfaced during this decision: the SESSION-CLOSE artefact is generated but never re-injected on resume (§9a). That is a real gap, small to fix (add one prompt contributor), and is logged as a Plan 4d item.

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
  CONTRACT.md          ← the manifest (see §3)
```

---

## 3. Versioning and the manifest

### Reuse the existing versioning mechanism

The artefact model already versions per `(ProjectId, FilePath)`. `GetArtefactVersionsQuery(ProjectId, FilePath)` returns all versions of one artefact; the latest version for a filePath is "the artefact." Each contract file is versioned by this existing mechanism — exactly like `REQ-001.md` is today. **No schema change, no new `v1/`/`v2/` directory hierarchy.**

### The coherence problem

Four files versioning independently means `API-CONTRACT.yaml` could be at v3 while `DB-SCHEMA.sql` is at v1. A downstream stage pinning "the contract" would have to track four version numbers and reason about which combination is coherent — reintroducing drift risk through the back door.

### The manifest (chosen solution)

A fifth artefact, `CONTRACT.md`, is a **manifest** that pins the exact version of each of the four files as a coherent set:

```
# Contract v3
API-CONTRACT.yaml   @ v3
DB-SCHEMA.sql       @ v1
DATA-MODELS.md      @ v2
ERROR-CATALOGUE.md  @ v2
```

- The manifest itself is a versioned artefact (its own filePath, its own version history).
- When any contract file changes in a breaking way, the manifest is bumped, snapshotting the new coherent set.
- Downstream stages pin **one number** — the manifest version — which resolves to four specific file versions.
- Each contract file stays in its native format, so NSwag and other tooling consume the real `.yaml`/`.sql` without tripping over anything.

**Rejected alternative:** one single artefact containing all four sections inline. Simpler to pin, but couples YAML + SQL + Markdown into one blob, breaks clean NSwag input, and makes diffing a single concern harder. The manifest is the ponytail answer — reuse the versioning that exists, add the minimum (one index file), keep each file native.

---

## 4. Breaking changes reuse the CHANGE-record pattern

A contract version bump is **not new machinery**. It reuses the existing CHANGE-record + domain-badge pattern already built for requirement changes:

- A contract change produces a new manifest version plus a `CHANGE-{id}.md` record.
- The CHANGE record carries **domain impact badges**: CS (Clinical Safety), IG (Information Governance), SEC (Security).
- A badge marks the corresponding downstream stage as requiring re-review.
- The same propagation that flags P06 when a REQ changes now flags P06 when the contract changes.

---

## 5. Resumption and staleness

The problem: a stage approved days ago against Contract v3; since then P04 issued v4. On reopening, the stage must not silently pick up v4 and quietly invalidate the approval.

### The pin

At approval, a stage records **which contract (manifest) version it was approved against**. This is the one genuinely new field required (on the stage/conversation).

### The staleness check on resume

On session resume, the stage compares its pinned manifest version against the latest manifest version. Three outcomes:

| Condition | Behaviour |
|---|---|
| Pinned == latest | Resume silently against the pinned version. |
| Pinned < latest, **no** domain badge for this stage | Resume against pinned version. Quiet note: "design has advanced to vN (no [domain] impact)." Human informed, not forced to act. |
| Pinned < latest, **domain badge hits this stage** | Open in re-review state. Banner: "Contract changed from vX to vN with a [domain] impact. Previous approval is stale. Review the delta." Show the CHANGE record — not the whole contract. |

This check runs on the existing per-turn prompt rebuild and feeds the existing `stalenessNotice` contributor (see §9a). Because the prompt rebuilds on every entry — including a resume after any elapsed time — the comparison re-runs whenever the user opens the chat. There is no persisted day-0 prompt to go stale.

### Why the badge is load-bearing

Without the badge, every contract bump would force every downstream stage to re-review. People drown in false alarms and start rubber-stamping — worse than no gate. The badge means a stage is pulled back **only** when the contract change actually touches its domain. A typo in the error catalogue does not drag six stages into re-review.

### Re-approval policy (open sub-decision)

When a stale-with-badge stage reopens, does re-approval require re-doing the stage or just acknowledging the delta? Proposed: **per-stage policy.** Clinical safety (P06) requires the CSO to actively re-approve against the new version — DCB0129 sign-off is personal and non-repudiable, so a diff acknowledgement is not enough; a lighter stage (e.g. P05) may accept acknowledgement. This mirrors the "which stages need assurance" policy already captured in P00.

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
3. A `CONTRACT.md` manifest pins a coherent set of file versions; downstream stages pin the manifest version (one number).
4. Breaking changes reuse the existing CHANGE-record + domain-badge pattern.
5. Resumption uses a pin (stage records approved-against manifest version) + a staleness check on resume; badge decides whether re-review is forced.
6. Tagging: P04 drafts mechanically from upstream anchors (marked draft); P06/P07/P08 role-holders ratify as a by-product of their existing assessment; ratification is a hard gate.
7. Tag corrections ride the existing feedback loop; accumulated corrections tune the P04 tagging skill; the ratification gate holds the safety floor regardless of draft accuracy.
8. Tags live in a structured traceability section in `DATA-MODELS.md`, not inline — because Genesis renders the role-holder worklist from it.
9. Three independent tag domains (CS/IG/SEC), each owned by its P00 role-holder, each triggering only its own re-review.

---

## 9a. Enforcement — DECIDED

**Decision: injection via the existing per-turn prompt rebuild, plus an optional cheap tool-layer backstop on the contract specifically.**

### What the codebase already does (verified, not assumed)

`ConversationStreamController.cs` rebuilds the system prompt **on every stream turn**, not once at conversation creation. The prompt is split into a stable/cached part and a mutable/fresh-each-turn part (for prompt-caching efficiency). The mutable part already has two contributors of exactly the shape this design needs: `stalenessNotice` and `handoverBlock` (both appended fresh each turn). The `get_artefact` tool call is already intercepted in a loop that extracts `file_path`, branches on path prefix, and can substitute its own tool result (currently used for read-budget enforcement).

This checks the two assumptions the earlier draft got wrong:
- **"Injection pattern doesn't exist" — false.** The per-turn rebuild with mutable contributors is robust and already live. Injection is real, not vapour.
- **"Contract-aware tool is heavy to build" — false.** The interception point exists; contract-awareness is one more branch in a loop that already inspects `file_path`.

### The mechanism

1. **Contract content → stable/cached part of the prompt**, injected at the *pinned* manifest version. Large, changes only on version bump — belongs in the cached part.
2. **Contract staleness → the existing `stalenessNotice`** (mutable part), computed during the per-turn rebuild. Because the prompt rebuilds on *every* entry — including a resume after 10 days — the pinned-vs-latest comparison runs every time the user opens the chat. A returning user whose contract moved to a new version with a domain badge gets the staleness banner on re-entry. There is no stale-prompt-replay risk: nothing persists a day-0 prompt.
3. **Tool-layer backstop — LOCKED IN.** Extend the existing `get_artefact` interception: when a `design/` path is requested inside a stage with a pinned manifest, resolve to the pinned version rather than latest. With the pinned contract already injected fresh every turn, the bypass this closes (agent ignores the in-context contract and calls the tool anyway) is low-probability rather than structural. Cost is near-zero (one branch on an existing loop); benefit is turning "low-probability" into "impossible" for the one artefact whose drift is a patient-safety issue. Adopted because the contract is the one artefact whose drift hurts a patient — the trivial cost is justified.

### The 10-day resume case (the test that drove this)

Come back after 10 days and reopen the chat. The prompt rebuilds on the next message regardless of whether this spawns a fresh conversation or continues an existing thread. The pinned contract is re-injected at its pinned version; the staleness check re-runs and fires the banner if the contract moved with a relevant badge. The point-of-use tool backstop (if adopted) guarantees the pinned version even if the agent reaches for the tool mid-session. All three entry/use points are covered.

### Related finding — SESSION-CLOSE re-injection gap (Plan 4d item)

While verifying the injection pattern, confirmed that the SESSION-CLOSE artefact is **generated, stored, and pushed to GitHub, but never read back into the prompt on resume.** Every code reference is generation/storage/push; `ConversationStreamController.cs` has no reference to it. The resume-summary that is purpose-built to tell the agent where to pick up is a write-only artefact.

This is the same shape as the DTO completeness failure: produced at one end, silently dropped at the other, all green. **Fix:** add a session-close contributor to the mutable part of the prompt (beside `stalenessNotice` and `handoverBlock`) that reads the latest SESSION-CLOSE artefact for the stage and injects it on resume. Small — one contributor, alongside two that already exist. Logged as a Plan 4d item, to be fixed before Plan 5 (the pipeline leans on clean session resume).

This finding also strengthens the argument for a **round-trip guardrail** (see §9b.2): for every artefact type meant to be re-consumed, a test proving it is actually read back, not just written.

**Scoped into this work.** The SESSION-CLOSE fix is not deferred to a separate task — it is the first, lowest-risk instance of the same injection-contributor pattern the contract enforcement uses. Building it first proves the pattern before the higher-stakes contract rides on it.

### Implementation sequence (within this work, once §9b resolved)

1. **SESSION-CLOSE contributor first.** Smallest, self-contained. Proves the "mutable-part contributor that reads an artefact and injects it on resume" pattern. Acceptance test: resume a stage, assert the session-close summary is present in the rebuilt prompt. Its own round-trip guardrail is the test that would have caught the original gap. Depends only on the round-trip guardrail being defined (§9b.2) — otherwise unblocked.
2. **Contract injection second.** Same pattern, now proven. Pinned contract in the stable/cached part; staleness in the mutable `stalenessNotice`. Depends on the round-trip guardrail *and* the TDD gate definition (§9b.1).
3. **Tool-layer backstop third.** The branch on the existing `get_artefact` interception.

All three follow TDD: tests first (RED), then the contributor/branch (GREEN), verified counts before commit.

---

## 9b. Open questions (next session)

**These block implementation. Resolve before any code.**

1. **TDD gate specifics (Plan 5).** Is the gate "contract exists and approved," or the stricter "contract manifest version matches the REQ version the tests trace to"? The stricter form guarantees the tests, the requirements, and the contract are all mutually consistent before code generation.

2. **Tag vocabulary specifics.** The traceability section location is decided (§6). The exact fields and format of a single tag entry are not (element identifier format, how an anchor is referenced, how draft/ratified state is represented).

---

## 9c. Guardrail set — DECIDED

**Scope: the broader silent-seam failure class, not only the contract layer.** Chosen deliberately as the better investment — the failure has bitten three times in one session and treating the root pattern is worth more than patching the contract instance alone.

### The failure class this targets

Three failures share one shape:

- **DTO gap:** handler computes a field → result carries it → response DTO omits it → HTTP body drops it. Green, because nothing tested the handler→HTTP seam.
- **SESSION-CLOSE gap:** artefact generated → stored → pushed → never read back into the prompt. Green, because nothing tested the write→resume seam.
- **Controller-completeness gap:** command + handler built → controller route never added. Green, because nothing tested the mediator→route seam.

The common shape is **not** "mapping" or "artefacts." It is: *a producer and a consumer are built in separate places (often separate sessions), each is internally correct and independently tested, and nothing tests the seam between them.* Unit tests pass on both sides because each side is coherent alone. The failure lives in the join — exactly what falls between two definitions of "done."

The guardrails are therefore **seam tests**: each asserts a specific producer→consumer handoff completes end to end. Not more unit tests on either side — tests that only pass if the connection holds.

### The minimum seam-test set

1. **Result → HTTP body completeness** (DTO seam). For every command/query result field, a test asserts it appears in the serialised HTTP response. Reflect over the result type; assert each property is present in the response contract. Catches the DTO gap as a class — a new result field with no DTO mapping fails automatically.

2. **Command → route existence** (controller seam). For every command/query type, a test asserts a controller route dispatches to it. Catches the missing-endpoint class.

3. **Artefact write → read-back** (re-consumption seam) — **stronger form (decided).** For every re-consumed artefact type, an integration test that *writes the artefact, resumes the stage, and asserts it is present in the rebuilt prompt.* Not the weaker "a read path exists" form — the stronger form is chosen because this is the class that just bit (SESSION-CLOSE), and the weak form can pass while the read path is broken in a way it never exercises. The set of re-consumed types is a **hard-coded list (decided)**, not a registry — ponytail-minimal for the ~4 current types (session-close, contract manifest, contract files, REQ). Revisit only if the list grows materially.

4. **Tool registration → wiring** (tool seam). Already exists as `ToolCallWiringTests` (every tool in `PipelineToolDefinitions` must have a wiring test proving `ExecuteToolCallAsync` handles it). Named here explicitly as a member of this family — it is the same class, and it shows the pattern already had one member before the family was named.

5. **Pin → resolution** (contract enforcement seam). For a stage with a pinned manifest version, a test asserts reading a contract file returns the *pinned* version, not latest. Proves the injection + backstop actually binds.

### Honest limitations (do not oversell)

- **A seam test only catches seams that have been enumerated.** This set closes the known recurring classes. It does not catch a novel seam type nobody has thought of. The value is converting a recurring surprise into a named pattern with a standard countermeasure — the standing rule is: *discovering a new class of seam failure means adding a new seam-test type to this set, not just fixing the instance.*
- **Reflection-based completeness tests (1, 2) need opt-outs for genuinely internal fields — and the opt-out becomes the new leak if ungoverned.** Rule: any completeness opt-out requires a reason string and is itself reviewed. Otherwise the leak has moved, not sealed.
- **Test 3 (stronger form) is the most expensive** — a full write-resume integration test per re-consumed type. Justified for this class; not a trivial add.

### The standing principle

Every producer→consumer seam in the pipeline has a test that fails if the handoff is incomplete. Discovering a new class of seam failure means adding a new seam-test type — not just fixing the instance. The five above are the starting members; the DTO and SESSION-CLOSE failures are why two of them exist; the wiring-test convention shows the family already had a member before it was named.

---

## 10. Sequencing note

The contract layer is Plan 4d item 1 and its design (item 17) must be signed off before implementation. Enforcement (§9a) and the guardrail set (§9c) are now decided. Plan 5 (TDD Agent) consumes the contract and cannot begin until the TDD gate (§9b.1) is defined.

**What is now unblocked:** the SESSION-CLOSE re-injection fix depends only on guardrail 3 (artefact write→read-back), which is now defined. It can therefore proceed under TDD as the first, lowest-risk instance of the injection-contributor pattern — ahead of the contract enforcement, which additionally needs the TDD gate (§9b.1). Remaining before contract implementation: TDD gate specifics and tag vocabulary (§9b).
