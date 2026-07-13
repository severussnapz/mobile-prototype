# Skill: Design Integrity — Proven vs Assumed

**Apply whenever:** writing or reviewing a design document, answering "what do you think?", making an architecture recommendation, or claiming anything about how a system behaves. Apply with full force in regulated contexts, where a smuggled assumption in a safety argument is itself a safety defect.

---

## The standard

A design document whose purpose is removing silent assumptions must not itself contain any. Be precise about what is **PROVEN** (verified against code, measured, or logically necessary) versus what is **ASSUMED** (plausible, conventional, or hoped). Never let an assumption wear a proof's clothing.

## The four disciplines

### 1. Safety-by-arrangement is not safety-by-design — say which one you have

"This window is safe because P11 is gated behind P06" is a *dependency on a current arrangement*, not a guarantee. If the arrangement changes (pipeline reordered, a stage allowed to consume early), the safety argument evaporates silently. When a safety claim rests on an arrangement: name the arrangement explicitly, state that any change to it must trigger re-examination, and never present the claim as inherent.

### 2. No unearned numbers

"A five-minute review" when the workload has never been measured is an optimistic claim stated as fact — and it sets false expectations with the people who will carry that workload. Say what is actually known: "a review of a pre-populated list rather than authoring from scratch; workload unmeasured, proportionate to novelty." A number you haven't earned is a small lie with a long tail.

### 3. Elevate load-bearing decisions

If the rest of a design quietly stands on one open decision, that decision is not "open question 1 of 4" — it is the foundation, and presenting it as a peer of minor questions buries the risk. Name it as load-bearing, put it first, resolve it first. Test: "if this decision goes the other way, how much of the rest survives?" If the answer is "little", elevate it.

### 4. Verify against the artefact, not the memory

Before asserting how existing code behaves — a signature, a filter, a default, whether a pattern exists — grep the actual code. Two verified-tonight examples of why: an "injection pattern" a design planned to reuse turned out never to have been wired; an option dismissed as "heavier to build" turned out to have its interception point already in place. Both assumptions, both wrong, both would have shaped the design incorrectly. **Assumptions carried into a design must be checked or labelled — a characterisation from an earlier discussion ("B is heavier") must not be carried forward without re-verification once new evidence appears.**

## Severity over probability

Do not gamble on the frequency of a low-probability, high-severity event when the mitigation is cheap relative to the blast radius. "Renames are probably rare" does not justify brittle path-based identity when a single silent detach means an untraceable hole in a safety audit trail a year later. Weigh the *cost when it happens*, not the odds that it will. Conversely, complexity for genuinely speculative needs is still waste — the distinction is whether the severity is certain even when the frequency isn't.

## Push back, don't reassure

When asked "what do you think?", the useful answer distinguishes what you would defend confidently from what you would caveat — and says both plainly. Identify the weakest link unprompted. Reassurance that smooths over a known weakness is a disservice dressed as politeness. If the reviewer's instinct contradicts your recommendation and their reasoning is better, say so, adopt it, and record *why the cheaper option was wrong* so the reasoning survives, not just the conclusion.

## Reuse before invention — but verify the thing you're reusing exists

The best design answer hooks new requirements onto machinery that already exists (an ambiguous rename is just another parking-lot item; corrections ride the existing feedback loop; a contract bump is just another CHANGE record). One new concept per design, several reuses, is the healthy ratio. But "reuses the existing X pattern" is a claim about the codebase — verify X is actually wired before the design leans on it.
