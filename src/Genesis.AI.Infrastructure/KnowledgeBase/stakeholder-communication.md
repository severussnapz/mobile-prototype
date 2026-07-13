# Skill: Stakeholder Communication & Decision Records

**Apply whenever:** writing an ADR, running a design review, presenting a technical trade-off to non-engineering stakeholders (CPO/CMO, CEO, PE owners), writing an investment or status narrative, or any moment where a technical judgement must survive translation into someone else's decision. The work isn't done when the engineering is right; it's done when the right people can act on it.

---

## ADR craft — writing for the reader two years out

An ADR's real audience is a future engineer (or auditor) deciding whether to *undo* the decision. Serve them:

- **Context first, decision second, consequences third** — and the context section must contain the constraints that *forced* the choice (scale, compliance, existing estate), because when those constraints change, the ADR tells you the decision is up for re-examination. An ADR without its load-bearing constraints is a conclusion without an expiry condition.
- **Record the rejected options and why each lost.** The pressure to "simplify" back to a rejected option always returns; the rejection reasoning is the immune system. "Path-based tag identity rejected: silent detach on rename is untraceable a year later" prevents relitigating it cheaply. Record *why the cheaper option was wrong*, not just that it lost.
- **Mark what is proven vs assumed** (design-integrity.md applies fully): verified-against-code claims, measured numbers, and hopes are different substances — an ADR that mixes them silently is a defect.
- **One decision per ADR, immutable once accepted** — superseded by a new ADR, never edited in place. Same discipline as migrations and artefact versions, for the same audit reason.
- **Status and provenance**: proposed/accepted/superseded, who decided, when, and — in regulated stages — which role-holder ratified. The ADR is an audit-trail entry, not a blog post.

## Running a design review that produces decisions

- **Pre-read, not presentation.** Circulate the design with its open questions *marked and prioritised* (load-bearing first); the meeting is for resolving the marked questions, not for discovering the design aloud. A review where attendees first encounter the design in the room produces reactions, not judgements.
- **Name the decision each agenda item needs** ("choose A or B on enforcement"; "sign off the tagging governance") — a review without named decisions ends in vibes.
- **The reviewer's dissent is data**: when a reviewer's instinct contradicts the recommendation, chase the disagreement to its underlying premise rather than defending. The best outcome of a review is a corrected design, and the record should credit the correction (the stable-ID decision exists because a review pushed back on "brittle").
- **Close with the written delta**: decisions made, by whom, what changed in the document, what remains open with owners. Unminuted reviews didn't happen.

## Translating trade-offs for non-technical decision-makers

The audience (a CPO/CMO ratifying clinical-safety process, a CEO, a PE owner) needs to make a *resourcing or risk* decision — give them exactly that shape:

- **Lead with the decision needed and its deadline**, then the two-or-three options, each stated as: what it costs (time, people, money), what risk it carries or retires, what it forecloses. Mechanism detail only on request.
- **Translate technical risk into consequence language**: not "path-based identity is brittle" but "a safety classification could silently stop tracking the thing it protects, and we couldn't prove to a regulator when it happened." The consequence is the shared vocabulary; the mechanism is yours.
- **Honest numbers or honest absence**: an unmeasured workload is presented as "unmeasured, proportionate to X, here's how we'd measure it" — never as a reassuring estimate (no "five-minute review" claims). Decision-makers burned by one optimistic number discount all your future ones.
- **Name what you're asking them to own.** "This adds a standing responsibility to the CSO function — that's a resourcing fact for you, not a footnote" is respect; burying it is how sign-offs get repudiated later.
- **Facts and honest assessment, no hyperbole** — a "responsible, human-led" positioning is destroyed faster by one inflated internal claim than by any competitor.

## Status narratives (upward reporting)

- Conclusion first: shipped / at-risk / blocked, then the one number or event that matters, then detail for those who read on. Bad news travels first and plainly — a risk reported early is a plan; the same risk discovered late is an incident.
- Velocity claims tie to the compounding measure (what this sprint's output makes cheaper next sprint), never vanity counts. Every ask is specific: a decision, a person, or a removal of a blocker — "for awareness" is a wasted slot.
