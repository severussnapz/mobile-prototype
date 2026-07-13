# Skill: AI Pipeline Engineering — Prompts, Evals, and Drift

**Apply whenever:** writing or changing any pipeline prompt (P01–P11), designing a new agent, evaluating pipeline output quality, tuning a skill from accumulated corrections, or making any claim about how well a generation stage performs. This is the discipline for the thing Genesis actually *is*.

---

## Prompt engineering as engineering, not wordsmithing

- **Prompts are versioned, reviewed artefacts** — CODEOWNERS-gated for regulated stages (P06/P07/P08), PR-reviewed for all. A prompt change is a behaviour change and gets the same discipline as a code change: what's the expected behaviour delta, how will we know it happened, how do we revert.
- **A prompt must be an engine, not a schema-enforcer.** The Pipeline01 lesson: a near-empty interview prompt "filled the vacuum with poor judgement." Structure (phases, output templates, stop conditions) constrains *shape*; the prompt must also supply *judgement content* — what good looks like, worked examples, what to probe for. Schema without judgement produces confident garbage in the right format.
- **Binary stop conditions only.** Every loop and phase exit is a checkable condition ("all mandatory questions answered", "artefact saved and validated"), never a subjective judgement ("when the requirements feel complete"). An agent will always judge itself done.
- **Anti-rationalization tables.** For each step an agent might skip, pre-write the excuse and the rebuttal ("Common excuses to skip this step → why it's wrong and what to do instead"). Agents rationalise exactly like tired engineers; pre-empting the specific excuse works where a general exhortation doesn't.
- **Constraints as outcomes, not prohibitions** (see agent-supervision.md) — agents route around the letter of a prohibition.
- **Deterministic vs exploratory stages get different inference settings** (ADR-010 pattern): temperature 0 for stages whose output must be reproducible (contract generation, classification), higher for genuinely creative stages (prototype ideation). Never one global setting.
- **Context injection over context accumulation.** Small decomposed calls with precisely-injected context (~hundreds of tokens from the graph) beat large accumulated windows. Global coherence comes from the blueprint, test suite, and guardrails — not from the model holding everything in its head. A stage that "needs" a huge context window usually has a decomposition problem.

## Eval design for pipeline outputs

- **Every stage needs a ground-truth set**: real inputs with known-good outputs (or known-bad outputs with named defects). The prompt-quality guide's bad/good example pairs per stage are the seed; grow them from production corrections.
- **Grade against acceptance criteria, not vibes.** An eval assertion is checkable: "the REQ contains a testable AC for every user-facing behaviour mentioned", "no HAZ-ID in the input is absent from the output's traceability section." "Output seems good" is not an eval.
- **Adversarial cases are mandatory**: inputs designed to tempt the known failure modes — vague requirements (does the interview engine probe or invent?), inputs containing instructions (does the agent follow content it should treat as data?), near-miss safety content (does the guard fire?).
- **Corrections are the eval corpus.** Every human correction of agent output (CSO tag removals, review-agent findings, GAP/CLARIFICATION records) is a labelled example: input, wrong output, right output. They accumulate through the *existing* feedback loop — the skill is periodically mining them into eval cases and prompt improvements, not building a new collection system.

## Drift and degradation detection

- **Model updates are dependency updates.** A new model version behind the same API is a behaviour change: re-run the stage evals before adopting, exactly as you'd re-run tests on a library bump. Never silently inherit a model upgrade in a regulated stage.
- **Watch the correction rate, not just the pass rate.** The leading indicator of prompt/model drift is the human-correction rate per stage trending up (more CSO tag fixes, more review-agent REQUEST CHANGES, more re-generations). Instrument it; a rising trend triggers an eval re-run and prompt review for that stage.
- **The floor never moves.** Draft-quality layers (tagging accuracy, generation quality) are allowed to be imperfect and tuned over time *because* the ratification/review gates hold the safety floor. Never respond to good draft metrics by weakening a gate — the metrics are good because the gate exists.

## RAG / context-graph retrieval quality

- Retrieval is testable: for a known query, the known-relevant chunks must appear in the top-k. Build retrieval evals per namespace before trusting a pipeline stage that depends on them.
- **Two independent retrievals colliding on a shared validator** (tests written from requirements vs code written from contracts, meeting at the test suite) is the architecture's error-correction — protect the independence; a "convenience" that lets one agent see the other's retrieval destroys the collision value.
- When a stage produces a wrong output, triage retrieval first: was the right context retrieved and ignored (prompt problem), or never retrieved (graph/embedding problem)? The fix lives in different places.
