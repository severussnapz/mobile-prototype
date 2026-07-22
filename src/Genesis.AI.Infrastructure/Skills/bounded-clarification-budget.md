---
name: bounded-clarification-budget
description: 'Use this skill in P03–P08 interview phases. Enforces the 8-question clarification budget, distinguishes proceed-with-assumptions from stop-for-blocker scenarios, and defines the parking lot escalation path.'
metadata:
  version: 1.0.0
  applyTo:
    - requirements
---

# Bounded Clarification Budget

## Budget: 8 questions per requirement per stage

Each requirement conversation has a maximum of 8 clarification questions across the entire stage.
This budget covers all phases combined — it is not 8 per phase.

Count applies to open-ended questions that require new human input.
Closed confirmations ("Any corrections before I advance?") do not count against the budget.

## Tracking

Maintain a running count. When you reach question 5, note internally that you have 3 remaining.
At question 8, you have exhausted the budget.

## When budget is not exhausted: proceed normally

Use questions judiciously. Prioritise questions that:
- Resolve a CRITICAL or HIGH parking lot item
- Determine a fork in the output structure (e.g. new service vs existing extension)
- Establish a fact that cannot be derived from prior artefacts

Do not use questions to confirm facts that are already in artefacts.

## When budget is exhausted: two paths

**Path A — Proceed with documented assumptions:**
If the remaining unknown can be reasonably derived from prior stage artefacts or EMIS-X standard patterns:
1. State the assumption explicitly: "Assuming [X] based on [source]. This is documented as PROPOSED."
2. Mark the output with `[PROPOSED — confirm before implementation]`.
3. Add a HIGH parking lot item: "Confirm [assumption] before implementation."
4. Continue.

**Path B — Stop for blocker:**
If the remaining unknown cannot be safely assumed:
1. Stop. Do not produce a potentially wrong output.
2. State: "I have reached the clarification budget for this stage. The following question is a blocker that cannot be safely assumed: [question]. Please answer before I continue."
3. Do not advance the phase.
4. Add a CRITICAL parking lot item.

## Which path to choose

Proceed with assumptions (Path A) when:
- The unknown is a detail within an already-confirmed approach (e.g. a specific field name)
- EMIS-X standard provides a clear default
- Being wrong costs a revision, not a compliance failure

Stop for blocker (Path B) when:
- The unknown determines a DCB0129 mandatory step
- The unknown is a lawful basis or data classification decision
- Being wrong could produce clinically unsafe output

---

## Anti-Rationalization Table

| Excuse | Why it is wrong | What to do instead |
|---|---|---|
| I have more questions - I'll ask them even though I've hit the budget | The budget exists to protect session time. | Exceed it only for genuine blockers, not curiosity. |
| I'll ask a clarifying question disguised as a statement | Implicit questions still consume budget and confuse the user. | Be explicit or park it. |
| This clarification is important enough to justify going over budget | Every question feels important. The budget forces prioritisation. | Park non-blockers. |
