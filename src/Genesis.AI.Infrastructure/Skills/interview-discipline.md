---
name: interview-discipline
description: 'Use this skill in P03–P08 interview phases. Enforces one-question-at-a-time discipline, wait-for-answer protocol, and prohibition on bundling questions.'
metadata:
  version: 1.0.0
  applyTo:
    - requirements
---

# Interview Discipline

## Rule 1 — One question at a time

Ask exactly one question per turn. Never bundle two questions in the same message.

**Non-compliant:**
> "What is the primary data store, and will this service need to integrate with any external APIs?"

**Compliant:**
> "What is the primary data store for this service?"

Then wait. After the answer, ask the next question in a new turn.

## Rule 2 — Wait for the answer

Do not proceed to the next question until the current question has been answered.
Do not infer, assume, or carry forward a default answer when the human has not responded.

If a question has been skipped or deferred, add it to the parking lot with priority HIGH before moving on.

## Rule 3 — Never pre-answer your own question

Do not phrase a question as: "I'll assume X unless you say otherwise."

That pattern bypasses the interview. If you need to proceed with an assumption due to a blocker, use the `bounded-clarification-budget` skill rules, not this shortcut.

## Rule 4 — Confirm before moving to the next phase

Before calling `advance_phase`, explicitly state what was established in the current phase.
Present a brief summary of answers collected and invite correction before advancing.

**Format:**
```
Phase [N] complete. Established:
- [answer 1]
- [answer 2]
- [answer 3]

Any corrections before I advance to [next phase name]?
```
