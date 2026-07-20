# Genesis AI — ROI Metrics Design (Output vs Outcome)

*Status: v0.2 — decisions closed, ready to commit to KnowledgeBase. Design session with Idris/Yas/Roel still needed to ratify and schedule build (Planned 1, genesis-ai-retro-actions.md row 11).*
*Owner: Idris Issa | Version: 0.2*

---

## 1. Why this split exists

Genesis AI produces **artefacts** (REQ, ARCH, DCB0129, IG, TDD test suites once Workstream D lands). It does not currently write application code — Workstream E (Code Swarm) is specified but not started. Engineers still write and merge the code, using the artefacts as input.

Collapsing "Genesis produced this artefact" and "this code shipped faster" into one metric overstates what Genesis is proven to cause. Any ROI model has to keep two layers separate:

- **Output** — what Genesis directly produces and controls. High confidence, directly instrumented.
- **Outcome** — what happens downstream in human-led build/ship. Genesis is one input among several (team familiarity, concurrent process changes, reviewer load). Correlational, not causal, until Workstream E exists.

Presenting these as one number would fail the standing rule in `stakeholder-communication.md`: honest numbers or honest absence, never a reassuring estimate.

---

## 2. Layer 1 — Output metrics (directly attributable)

**What it measures:** time and cost for Genesis to take a capability from P01 start to an approved artefact set (REQ + ARCH + DCB0129 + IG, and TDD suite once Workstream D exists).

**Source:** Genesis DB only. Already captured per `genesis-ai-retro-actions.md` Planned 1 — this is a query/visualisation problem, not a collection problem.

| Metric | Source field(s) | Confidence |
|---|---|---|
| Time per stage, P01→approval | Stage completion timestamps | High — instrumented today |
| Token cost per stage / per capability | Token usage per stage | High — instrumented today |
| Artefact push success rate | Artefact push logs | High — instrumented today |
| Pipeline completion rate | Conversation/session history | High — instrumented today |
| Rework rate (GAP/CLARIFICATION/CONTRADICTION count post-approval) | Feedback loop classification | High — already structured data |

**Baseline for this layer — DECIDED: retrospective estimate.** Pre-Genesis, this was manual workshop time (BA + architect + CSO hours) per capability, not systematically logged anywhere today. Approach: a small sample of recent pre-Genesis capabilities, hours estimated by the **EM and Product Lead**, reported as a **blended rate card** figure (decision 4 below) rather than role-by-role precision. Labelled explicitly as *estimate*, not *measurement*, everywhere it's presented — per `stakeholder-communication.md`'s "honest numbers or honest absence" rule.

**Mechanics still open:** sample size — see §5.

---

## 3. Layer 2 — Outcome metrics (downstream, confounded)

**What it measures:** capability-level SDLC cycle time from ticket-open through PR-merged to deployed, for capabilities whose artefacts came from Genesis vs capabilities documented the traditional way.

**Sources:**
- **Plandek** (GitHub-native cycle time, lead time, PR review time, deploy frequency) — **DECIDED: use existing Plandek history as-is**, no backfill effort. Limitation to flag: the pre-Genesis window is whatever history happens to already be in Plandek, not a deliberately chosen baseline period — if that window turns out to be short or thin, the baseline confidence is correspondingly weaker. Worth a quick check of actual coverage before relying on it for anything reported externally.
- **Genesis DB** — supplies the "was this capability's artefact set Genesis-produced" flag, keyed by REQ-ID/capability ID
- **GitHub — REQ-ID↔PR linkage, DECIDED (ponytail): extend the existing convention, don't build new infrastructure.** `GENESIS-010` already enforces conventional commits (`type(scope): description`) via the review agent, and branch naming already follows `plan{n}-{feature}-exp`. Cheapest fix: carry the REQ-ID in the existing scope field (`feat(REQ-042): ...`) rather than building a mapping table, webhook, or new join service. This rides on machinery already enforced at PR-gate level (`review-agent-github-ci.md`), so it costs a convention update, not new tooling. Plandek groups by branch/PR-title pattern already, so this should be sufficient for it to segment without custom integration. Revisit only if a first pass shows the convention isn't followed consistently enough to join on reliably.

**Cohort definition — corrected from the earlier draft:** the split is **capability-level, by artefact provenance**, not GitHub commit-author. `genesis-ai-bot` commits mark artefact pushes to `.genesis/` paths, not application code, so bot-authorship is not a valid proxy for "AI-touched code" at this stage. Using it as one would silently smuggle an output claim into an outcome number.

| Metric | Interpretation caveat |
|---|---|
| Cycle time: ticket-open → PR-merged | Correlational. Team ramp-up, concurrent process changes, and reviewer familiarity all confound a naive before/after — match cohorts on capability size/complexity where possible, don't just split by calendar date |
| PR review comment count | Proxy for artefact clarity reducing review friction |
| Requirement-clarification round-trips during build | Proxy for artefact completeness — did engineers still have to go back and ask what was meant |
| Defect rate post-artefact vs pre-artefact | Proxy for artefact quality, not speed |

**When this becomes a clean causal measurement:** once Workstream E (Code Swarm) is live and `genesis-ai-bot` (or its successor) is actually authoring application code. At that point Plandek's existing AI-tool attribution (it already segments Copilot/Cursor/Devin-touched PRs) becomes directly usable, and the cohort-by-bot-authorship approach from the earlier draft becomes valid — it isn't valid yet.

**Reporting decision — DECIDED:** Layer 2 is reported internally as a directional trend (useful for spotting problems early), but is explicitly **excluded from any £ ROI figure or spend justification** until Workstream E removes the human-execution confound. The concern driving this — not wanting a confounded number to justify spend it doesn't actually support — is exactly the failure mode this split is designed to prevent. Track it, don't fund against it.

---

## 4. Combined ROI expression (once both layers have real numbers)

Kept explicitly as two separate claims, not blended into one ratio:

    Layer 1 (Output ROI):
      £_saved_artefact_time = (baseline_artefact_hours_est − genesis_artefact_hours) × blended_rate
      £_invested_layer1      = Σ token cost per stage + infra run cost
      → ratio, reportable once the retrospective estimate sample is done — clearly
        labelled as resting on an estimated baseline, not a measured one

    Layer 2 (Outcome signal, NOT converted to £ or used for spend decisions):
      cycle_time_delta = plandek_baseline_cohort_cycle_time − genesis_artefact_cohort_cycle_time
      → reported internally as a directional trend only, with confound caveat attached,
        until Workstream E removes the human-execution confound. Never cited as
        justification for spend on its own.

---

## 5. Decisions (resolved)

1. **Plandek coverage** — use existing history as-is, no backfill. Coverage window to be checked before external reporting.
2. **Layer 1 baseline** — retrospective estimate from a small sample of recent pre-Genesis capabilities.
3. **REQ-ID↔PR linkage** — extend the existing `GENESIS-010` conventional-commit scope field to carry the REQ-ID, rather than building new join infrastructure. Ponytail-minimal: reuses an already-enforced convention.
4. **Cost conversion** — blended rate card, not role-by-role rates.
5. **Layer 2 reporting** — visible internally as a directional trend; explicitly excluded from any £ ROI claim or spend justification until Workstream E removes the confound.

**Remaining scoping item — DECIDED: estimators are the EM and Product Lead.** Sample size still open (a handful of recent pre-Genesis capabilities is enough to be indicative — exact count is a scoping conversation with them, not a design decision).

---

## 6. Fit with existing plan

This is the design input for **Planned 1 — Project Dashboard (KPIs and OKRs)** (`genesis-ai-retro-actions.md`, row 11), which is already logged as needing a design session before build, and already notes the underlying Genesis-side data is captured — this doc scopes what to add (Plandek/GitHub join) and how to keep the two claims honestly separated.
