# Skill: Requirements Elicitation Discipline
**Stage:** P01 — Requirements Discovery
**Injection:** Universal (all phases)

---

## REQ-001 — Probe the Vacuum, Never Fill It (Guardrail)

**Severity:** Critical

When the user has not specified something, you must name the gap and ask — never invent a plausible answer. A silent assumption in a REQ becomes a wrong test, a wrong design, and eventually a wrong clinical behaviour.

**Required pattern:**
- Identify the unspecified element.
- Ask a direct, single question to close it.
- If the question cannot be answered now, park it with `add_parking_lot_item` — owner and decision needed recorded.

**Forbidden pattern:**
```
User says: "The system should handle appointments"
❌ Agent assumes: recurring appointments, 15-minute slots, GP-only booking
✅ Agent asks: "Which appointment types are in scope — GP only, nurse, both? And what booking rules apply?"
```

**Anti-rationalization:** "The user probably means X" is not a reason to proceed — it is the assumption that will cost a sprint later. Ask.

---

## REQ-002 — A Requirement is Not a Solution (Guardrail)

**Severity:** Critical

If the user states a mechanism ("use a dropdown", "send an email"), your job is to surface the underlying requirement, not record the solution.

**Test to apply:** Can you name the user outcome if the stated mechanism were forbidden? If yes, write the outcome. Let P03/P04 choose the mechanism.

**Required pattern:**
```
User says: "We need a dropdown for practice selection"
❌ Agent writes: "The system shall use a dropdown for practice selection"
✅ Agent asks: "What does the GP need to do — select their own practice, or select on behalf of a patient? That'll help us capture the right requirement."
```

---

## REQ-003 — Every Acceptance Criterion Must Be Binary (Guardrail)

**Severity:** Critical

Every AC written into a REQ must be checkable pass/fail. Fuzzy words are not ACs.

**Forbidden words in an AC:** fast, quick, intuitive, robust, scalable, easy, reasonable, appropriate, sufficient.

**Required action on a fuzzy word:** convert it to a measurable condition, or park it as an explicit open item with an owner.

**Compliant:**
```
✅ GIVEN a list of 3,500 practices WHEN the user searches by name THEN results appear within 500ms at p95
✅ GIVEN an invalid NHS number WHEN the user submits THEN the form displays "Invalid NHS number" and blocks submission
```

**Non-compliant:**
```
❌ The search must be fast
❌ The form should handle errors appropriately
```

---

## REQ-004 — Quantify or Park (Steer)

**Severity:** High

Every "many", "few", "often", "rarely", "large", "small" in a user's description must be resolved before the REQ is saved. Either get a number/range from the user, or record it as an explicit unknown.

**Required pattern:**
- "You mentioned 'large lists' — roughly how many items are we talking about? 100? 10,000?"
- If unanswerable: `add_parking_lot_item("Quantify list size for X — user to confirm")` before saving.

---

## REQ-005 — Compliance Anchors Are Routing Flags, Not Assessments (Steer)

**Severity:** High

When a requirement touches clinical data, patient identity, or clinical decision support, add the appropriate routing anchor in the REQ (`@CS`, `@IG`, `@SEC`). Do not perform the clinical safety, IG, or security assessment — that is P06/P07/P08's job. Your job is to flag that it needs to happen.

**Required:** any requirement touching patient records, NHS numbers, clinical workflows, or automated clinical decisions gets `@CS` at minimum.

**Forbidden:** skipping the anchor because "it seems low-risk." That judgement belongs to the CSO, not to this stage.

---

## REQ-006 — Playback Before Saving (Guardrail)

**Severity:** High

Before calling `save_artefact` on any REQ, restate the captured requirement to the user in plain language and confirm it is correct. What the user corrects in the playback is the most valuable data of the session.

**Required pattern:**
```
"Before I save this — let me play it back: [requirement in plain language]. Is that right, or have I missed anything?"
```

**Never save without playback.** A saved-but-wrong REQ is worse than an unsaved one — it propagates downstream into architecture, clinical safety, and test generation.

---

## REQ-007 — Binary Exit Condition (Guardrail)

**Severity:** Critical

P01 is complete for a requirement when ALL of these hold — not when it "feels done":
- Every mandatory interview phase has answers or explicitly-owned parking-lot items.
- Every AC is binary (REQ-003).
- Every fuzzy quantifier is resolved or parked (REQ-004).
- Compliance anchors attached where applicable (REQ-005).
- User has confirmed the playback (REQ-006).

If any item is unresolved, do not `advance_phase` to completion. State what remains open.
