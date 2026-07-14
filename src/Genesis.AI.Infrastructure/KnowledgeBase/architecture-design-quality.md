# Skill: Architecture & Design Quality Judgement

**Apply whenever:** proposing, evaluating, or reviewing a design — a new service, a new aggregate, a workstream architecture, a pipeline stage, or any change that introduces structure. Apply BEFORE implementation is scoped; a design flaw found at review costs hours, found in production costs a migration.

---

## The questions that separate a good design from a plausible one

### Boundaries: who owns what?
Every piece of data and every behaviour has exactly one owner. Test a proposed boundary by asking: "when this data changes, how many components must change with it?" If the answer is more than one, the boundary is in the wrong place. In Genesis terms: the artefact aggregate owns artefact state; the manifest owns pins; a design where the staleness check reaches into artefact internals rather than asking the manifest has crossed a boundary.

### Coupling: what breaks when this changes?
For every dependency a design introduces, name what happens when the depended-on thing changes. Afferent coupling (who depends on me) determines blast radius; efferent coupling (what I depend on) determines fragility. A component with high both is a refactor trap. Prefer depending on stable abstractions (interfaces owned by the domain) over concrete infrastructure.

### Sync vs async: what does the caller actually need?
Synchronous when the caller cannot proceed without the answer. Asynchronous (event, queue) when the caller only needs the work to happen eventually — and can tolerate it not happening immediately or needing retry. The Genesis pattern is instructive: artefact GitHub push is a domain-event side effect that never blocks indexing (correct — the user doesn't wait on GitHub); artefact save is synchronous (correct — the user needs the confirmation). A design that makes the user wait on a best-effort side effect, or fires-and-forgets something the user needs confirmed, has this backwards.

### Abstraction: is it earning its keep?
An abstraction is justified when there are two real implementations today, or one implementation plus a *certain* (not speculative) second. Interface-per-class as a reflex is noise. The ponytail test applies: does this need to exist? The counter-test: is the absence of this abstraction forcing duplication or leaking infrastructure into domain code? Either answer decides it — "might need it later" decides nothing.

### The one-new-concept ratio
A healthy design introduces one genuinely new concept and reuses several existing mechanisms. The contract layer design was the model: one new thing (draft/ratified tag state), four reuses (P01 hazards, CHANGE records, CODEOWNERS gate, feedback loop). A design introducing three new mechanisms where one plus reuse would do is over-designed; interrogate each new mechanism with "what existing machinery almost does this?"

## Structural smells worth naming in review

- **A component that must be constructed two ways** (with and without a dependency) — the design is trying to serve two contexts; split it or make the dependency universal.
- **Configuration that changes behaviour semantics** (not just tuning) — two behaviours wearing one component's name; the flag will eventually be half-flipped somewhere.
- **A "manager", "helper", or "service" whose name doesn't say what it does** — usually a boundary that couldn't be drawn, hiding as a grab-bag.
- **Symmetric-looking operations with asymmetric guarantees** (save is transactional, delete is best-effort) — the asymmetry must be loud in the design, or callers will assume symmetry.
- **Anything whose correctness depends on call order that nothing enforces** — make the type system or the aggregate enforce it, or document it as a known trap with a test.

## Decision recording

Every load-bearing choice gets an ADR-shaped record: the decision, the options rejected, and *why the rejected ones were wrong* — because a year later the pressure to "simplify" back to a rejected option returns, and the record is the immune system. (See stakeholder-communication.md for ADR craft.)
