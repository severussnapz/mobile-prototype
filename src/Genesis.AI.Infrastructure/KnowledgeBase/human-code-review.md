# Skill: Human Code Review Craft

**Apply whenever:** a human reviews a PR — as tech lead, peer, or the second pair of eyes on agent-generated code that already passed automated gates. The automated Review Agent checks rules; the human checks what rules can't express. These are different jobs; this skill is the human one.

---

## What only the human can check (spend your attention here)

The Review Agent covers standards, shortcuts, and pattern violations. The human's unique value:

- **Does this change do the right thing, not just a thing correctly?** Read the linked REQ/issue first, then the diff. The most expensive defect a review can miss is a correct implementation of the wrong behaviour.
- **Is this the right *size* of solution?** Ponytail judgement: could three of these five new files be one? Is the new abstraction earning its keep, or is it speculative structure? Automation flags rule violations; over-engineering within the rules is a human call.
- **What does the diff *not* contain that it should?** Missing edge-case handling, the absent test for the path that matters, the migration that should accompany the schema change, the second call site that needed the same fix (root cause vs symptom). Absence is invisible to pattern-matchers; hunting it is the reviewer's core skill.
- **Will the next person understand this?** Naming, decomposition, and whether the commit messages tell the story. "It works" and "it's maintainable" are different bars.

## The review method

1. **Read the intent first** — PR description, linked REQ, the tests' names. Form an expectation of what the diff should contain *before* reading it. Divergence between expectation and diff is where findings live.
2. **Read tests before implementation.** Tests are the claimed contract; then check the implementation honours it — and check the tests actually assert it (real assertions, real captures, no mirrored implementation). A green suite proving nothing is worse than a red one.
3. **Walk one unhappy path end to end** by hand: pick the most likely failure (null, timeout, missing artefact, concurrent edit) and trace what the user experiences. Silent failure or raw exception reaching the user = finding.
4. **Grep beyond the diff** when the change fixes a bug: is the same defect in sibling code? A fix that patches one of three identical call sites is a symptom fix.
5. **Check the seams** the change introduces (see seam-testing.md): every new producer has a consumer, every new field crosses the HTTP boundary, every new artefact is read back.

## Giving feedback that lands

- **Severity-tag every comment** (blocker / should-fix / nit) so the author knows what gates the merge. A wall of undifferentiated comments teaches authors to ignore all of them.
- **State the why, propose the fix.** "This leaks the raw exception to the client (userMessage pattern violated) — map it in the handler like `UpdateProjectCommand` does" beats "don't do this."
- **Distinguish taste from defect, out loud.** "Nit, feel free to ignore: I'd name this X" keeps trust; taste dressed as a blocker burns it.
- **Ask genuine questions where you're unsure** — "what happens if two users hit this concurrently?" is a review finding even when the answer turns out to be "it's fine": now it's *known* fine.
- **Praise specifically, sparingly, honestly** — one "this decomposition is exactly right" where it's true does more for the codebase than reflexive positivity. Never praise as padding.

## Reviewing agent-generated code specifically

- The author cannot learn from your feedback — but the *prompt and instructions can*. Every human finding on agent code is a candidate rule: recurring finding → new line in copilot-instructions / the Review Agent prompt. Review-as-mentorship becomes review-as-tuning.
- Trust nothing about effort signals. Human heuristics ("they clearly thought about this") misfire on fluent generated code — volume and polish are free for agents. Apply the full verify-don't-trust audit (agent-supervision.md) regardless of how considered the diff looks.
- Watch for *plausible completeness*: agent diffs characteristically look finished — the missing piece (the second call site, the rollback path, the read-back) is the classic agent gap.

## Reviewer hygiene

- Cap continuous review at ~400 lines or ~60 minutes; beyond that, findings-per-line collapses. Split large PRs or take breaks — and push back on PRs too large to review honestly: "I can't review this responsibly at this size" is a legitimate blocker.
- Never approve what you didn't read. "LGTM" on a skim is a signature on a document you didn't check — in this codebase, sign-offs are audit-trail entries.
