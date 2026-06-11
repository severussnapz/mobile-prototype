# SKILL: user-flow-mapping
# Phase: P05 Product Experience Design — Phase 1

## User Flow Mapping

**Purpose:** Map user journeys from entry point to outcome for each requirement.

### Fast-Track Rule (Prototype Constraint)

If ROUTING CONTEXT `prototype_present: true`: take confirmed user flows from the prototype as authoritative. Only design flows for gaps or new requirements not covered by the prototype. Never redesign an accepted flow.

### For Each Requirement

Answer:
1. "Who is the user?" → Clinician, Admin, Patient, System
2. "What triggers this flow?" → Event, navigation action, external trigger
3. "What is the successful outcome?" → What the user sees/achieves
4. "What are the error paths?" → Validation failure, permission denied, not found
5. "Are there decision points?" → Branching based on state, role, or data

### User Flow Template

```markdown
### User Flow: {FlowName}

**Actor:** {User type}
**Trigger:** {What starts this flow}
**Happy path:**
1. {Step 1}
2. {Step 2}
3. {Step 3} → **Outcome:** {What user sees}

**Error paths:**
- {Error condition} → {User message / recovery action}

**Decision points:**
- {If condition} → {Branch A / Branch B}
```

### Validation

```
"User flow for REQ-{NNN}:
- Actor: {type}
- Steps: {N}
- Error paths: {M}
- Matches API contract endpoints from P04: Yes/No

Correct?"
```
