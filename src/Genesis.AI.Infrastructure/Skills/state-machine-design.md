# SKILL: state-machine-design
# Phase: P04 Design — Phase 4

## State Machine Design

**Purpose:** Design state transitions for complex multi-step workflows. This phase is NEVER auto-skipped — state machines are requirement-specific decisions.

### When This Applies

Use this skill when a requirement has:
- Multiple named states (Draft, Submitted, Approved, etc.)
- User or system actions that trigger state changes
- Business rules about which transitions are valid
- Side effects on transition (notifications, audit log, timestamps)

### State Machine Template

```csharp
public enum {AggregateType}State
{
    {State1},
    {State2},
    {State3}
}
```

Transitions table:

| From | Event | To | Side Effects |
|------|-------|-----|-------------|
| Draft | submit | Submitted | Notify reviewer, audit log |
| Submitted | approve | Approved | Notify applicant, set approved_at |
| Submitted | reject | Rejected | Notify applicant, require reason |

### API Transition Endpoint

Design a transition endpoint for each user-triggered event:
```
POST /api/v1/{resource}/{id}/{event}
```

Example: `POST /api/v1/prescriptions/{id}/submit`

### Validation Rules on Transitions

Specify which transitions require validation:
- e.g. "Cannot approve if required fields are blank"
- e.g. "Cannot cancel after Dispensed"
- e.g. "Rejection requires a non-empty reason"

### Validation

```
"State machine for REQ-{NNN}:
- States: {list}
- Transitions: {From → Event → To}
- Transition endpoints: {list of POST paths}
- Side effects: {list}
- Validation rules: {list}

Correct?"
```
