# Skill: Requirements Discipline — Elicitation, REQ Craft, and P01 Judgement

**Apply whenever:** running or reviewing a P01 session, writing or assessing a REQ artefact, judging whether a requirement is ready to flow downstream, or designing interview-engine prompt content. The pipeline's output quality is capped by P01's — this is the highest-leverage stage.

---

## A requirement is not a solution

The most common defect in a REQ is a solution wearing a requirement's costume: "the system shall use a dropdown for practice selection" is a design decision; the requirement underneath is "a user must be able to select their practice from ~3,500 without error." The test: **can you name the user outcome if the stated mechanism were forbidden?** If yes, write the outcome and let P03/P04 choose the mechanism. If no, you don't have a requirement yet — keep eliciting.

## The anatomy of a testable REQ

Every requirement must survive these checks before it flows downstream:

- **Actor named** — who does/experiences this? "The system" is not an actor; a role is (GP, practice admin, CSO).
- **Observable outcome** — what is true afterwards that wasn't before, stated so a test could assert it.
- **Acceptance criteria that are binary** — each AC is checkable pass/fail. "Fast", "intuitive", "robust" are not ACs; "search returns within 2 seconds at p95 for a 3,500-practice list" is. Every fuzzy word gets converted or explicitly parked with an owner.
- **Boundaries stated** — what is explicitly out of scope, and the edge cases: empty states, maximums, concurrent actions, the unhappy paths. A REQ that only describes the happy path is half a REQ.
- **Traceable anchors attached** — hazards (HAZ-IDs), compliance touchpoints (CS/IG/SEC routing flags) identified at *anchor* depth: enough for downstream stages to know they're implicated, without doing their deep work for them. P01's compliance phases are lightweight routing, not the assessment.

## Elicitation craft (what the interview engine — human or agent — actually does)

- **Probe the vacuum, never fill it.** When the stakeholder hasn't specified something, the failure mode is inventing a plausible answer (the Pipeline01 lesson). The discipline: name the gap, ask the question, and if unanswerable now, record it as an explicit open item with an owner — never a silent assumption.
- **Ask for the story, not the feature.** "Walk me through the last time this went wrong" surfaces real requirements; "what features do you want" surfaces a wishlist. Concrete recent incidents beat hypotheticals.
- **Chase the why-chain twice.** A stated need is usually a means to an unstated end; two "what does that let you do?" hops typically reach the real requirement — and often reveal a cheaper way to meet it.
- **Quantify or park.** Every "many", "quickly", "rarely": either get a number/range from the stakeholder, or park it as an explicit unknown. Unquantified words become silent assumptions in P04.
- **Surface conflicts early.** Two stakeholders' requirements that cannot both hold (or a requirement conflicting with a compliance constraint) is a *product decision*, not something to average away in wording. Route it to the decision gate (parking lot / pre-swarm decision surface) — the pipeline's human-intervention machinery exists for exactly this.
- **Play it back in their words.** Close each elicitation loop by restating the requirement to the stakeholder for confirmation. What they correct in the playback is the most valuable data of the session.

## Change is normal; silence about change is not

Requirements will move after approval. The discipline is that movement is always loud: a change produces a CHANGE record with domain badges, downstream stages get flagged per the staleness model, and nobody discovers a moved requirement by noticing the code disagrees with the doc. "The REQ quietly edited in place" is the corruption this whole system exists to prevent — versioned, immutable, badged change is the only kind.

## Judging P01 completeness (the binary exit)

P01 is done for a feature when: every mandatory interview phase has answers or explicitly-owned open items; every AC is binary; every fuzzy quantifier is quantified or parked; hazard/compliance anchors are attached; conflicts are routed, not averaged; and the stakeholder has confirmed the playback. That list is checkable — which is what makes it a gate rather than a feeling.
