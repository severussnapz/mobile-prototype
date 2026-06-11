# SKILL: cross-requirement-chain
# Phase: P04 Design — Phase 1

## Cross-Requirement Chain Detection

**Purpose:** Detect when requirements form an orchestration chain and design the coordination mechanism.

### Detection Questions

For each requirement, ask:
1. "Does this requirement produce data that another requirement consumes?"
2. "Do multiple requirements need to execute in a specific order to satisfy a user outcome?"
3. "Is there a multi-step workflow that crosses service boundaries?"

### Patterns and Design

**BFF Aggregation** — when a frontend needs data from multiple services in one request:
- Design a BFF (Backend for Frontend) endpoint that calls internal services and aggregates
- BFF endpoint in its own slice: `GET /api/v1/bff/{screen-name}`
- Never expose raw service-to-service calls to the frontend

**Saga / Choreography** — when steps must complete in order with compensation:
- Design command events (EventBridge or SQS): `{ServiceName}.{AggregateType}.{EventType}`
- Each step listens to upstream event, performs work, emits downstream event
- Compensation: each step must have a compensating action if a later step fails

**State Machine** — when a single aggregate transitions through states driven by multiple actors:
- See `state-machine-design` skill
- Design state transition API: `POST /api/v1/{resource}/{id}/{action}`

### Chain Documentation Template

```markdown
### Cross-Requirement Orchestration

**Chain:** REQ-001 → REQ-003 → REQ-005

**Pattern:** {BFF Aggregation | Saga | State Machine}

**Coordination:**
- REQ-001 produces: {event/output}
- REQ-003 consumes: {event/input}, produces: {event/output}
- REQ-005 consumes: {event/input}

**Failure handling:**
- If REQ-003 fails: {compensation action}
- Retry policy: {exponential backoff, max attempts}
```
