# Genesis AI — Module 3: Architecture
## Role: Architect | Est. 45 minutes
### Prerequisite: Module 0 complete

---

## What This Module Covers

P03 and P04 are where you define the system architecture for an increment — service boundaries, integration patterns, data flows, API contracts, DB schema, and the ADRs that explain why each decision was made.

The agent comes into P03 having already read the approved REQ file, the prototype, and the P00 project context. It is not starting from scratch. Your job is to guide the architectural decisions, challenge the agent's proposals, and record the rationale so that every future stage — including the TDD agent and the code swarm — has a precise, defensible specification to work against.

---

## What P03 and P04 Produce

**P03 — Architecture:** `architecture/ARCH-{id}.md`
- Service decomposition
- Integration patterns (sync/async, event-driven)
- Data flow diagrams
- Architectural Decision Records (ADRs)
- Technology stack decisions
- Non-functional architecture (resilience, observability, security posture)

**P04 — Design:** Appended to ARCH or as a companion document
- OpenAPI/REST contracts per endpoint
- EF Core entity definitions
- Flyway migration SQL
- Index strategy
- Constraint definitions

Every item in the REQ file's DB Schema and Component Interfaces sections must be implemented here. If you add a new interface or table in P03/P04 that wasn't in the REQ file, that is an architectural discovery — raise a `propose_requirement_change` to update the REQ file accordingly.

---

## What the Agent Already Knows

When you start P03, the agent has read:
- The full approved REQ file (requirements, ACs, CHECKs, HAZ-IDs, DB schema, component interfaces)
- The approved prototype (what the user experience looks like)
- The P00 project context (release type, assurance requirements, stakeholders)

It will use these as constraints. It will propose an architecture that satisfies every AC in the REQ file. Your job is to verify that it does, and to challenge any proposal that conflicts with EMIS-X architectural standards.

---

## EMIS-X Architectural Standards

These are non-negotiable constraints the agent is aware of. If it proposes something that conflicts, challenge it:

- **DDD aggregates** — domain entities grouped by consistency boundary, not by table
- **Repository pattern** — all DB access through typed repositories, no raw EF queries in controllers
- **CQRS** — commands and queries separated at the handler level (MediatR)
- **Event-driven integration** — between services via domain events, not direct service-to-service calls
- **PrivateLink** — all inference through AWS Bedrock via PrivateLink. Nothing leaves the VPC
- **Flyway migrations** — all schema changes via versioned SQL migrations, never EF migrations
- **No global snake_case convention** — explicit `ToTable()` and `HasColumnName()` in every EF entity configuration
- **TimeProvider injection** — never `DateTime.UtcNow` directly
- **Soft deletes** — `IsDeleted` flag on domain entities, no hard deletes

---

## Exercise 1: Read the Existing Architecture Artefact

1. Open Genesis AI → Projects → "GP Appointment Reminders (Training)"
2. Click on the Artefacts tab
3. Open `architecture/ARCH-001-appointment-reminders.md`
4. Read through the service decomposition and ADR sections

**What to notice:**
- Every ADR has a context, decision, rationale, and consequences section
- The service boundaries follow DDD aggregate rules
- The integration pattern between the notification service and the SMS provider is event-driven, not synchronous
- There is an explicit ADR for why the notification job is a background service rather than a real-time trigger

**Question to answer:** The REQ file requires delivery within 60 seconds of the reminder job running. The architecture uses an async event-driven pattern. Can you spot the potential tension between these two requirements and how the architecture resolves it?

---

## Exercise 2: Challenge an ADR Proposal

1. Open a P03 conversation on the test project
2. When the agent proposes an ADR for the SMS provider integration pattern, challenge it:

> "You've proposed an async event-driven integration with the SMS provider. The REQ file requires delivery within 60 seconds. Async patterns can introduce latency. Justify why this is the right choice over a synchronous call."

3. The agent will provide its justification — evaluate whether it is sound
4. If you disagree, say so and propose an alternative

**Key learning:** The agent's first architectural proposal is a starting point. ADRs exist because the decision is non-obvious. If the rationale does not convince you, it will not convince a security reviewer or an auditor.

---

## Exercise 3: Raise a Requirement Change from an Architectural Constraint

During your P03 session, you will discover that the REQ file does not specify what happens when the SMS provider is unavailable. The architecture needs a retry policy, but there is no AC for it.

1. In the P03 conversation, raise this: "The REQ file does not specify retry behaviour when the SMS provider returns a 503. The architecture needs a retry policy. I need to raise a requirement change."
2. The agent will help you formulate the CHANGE record
3. Review the proposed AC addition and approve it

**What happens:**
- A CHANGE record is created: `requirements/CHANGE-001-sms-retry-policy.md`
- The REQ file is amended to include the new AC
- The CHANGE record is committed alongside the REQ file
- P03 continues with the updated requirement as a constraint

This is the correct way to handle architectural discoveries. Never silently add behaviour that is not in the REQ file — it will not have a test, and it will not have a clinical safety assessment.

---

## ADR Format

Every ADR must follow this structure:

```markdown
## ADR-{n}: {Decision title}

**Context:** What situation or constraint prompted this decision?

**Decision:** What was decided?

**Rationale:** Why was this option chosen over the alternatives?
List the alternatives considered.

**Consequences:** What does this decision commit us to?
What is harder to change later because of this decision?
What monitoring or guardrails does this decision require?
```

### Example ADR

```markdown
## ADR-003: Notification job runs as BackgroundService, not as a scheduled AWS Lambda

**Context:** The reminder notifications must run at a defined time window (48 hours
before appointment). The EMIS-X infrastructure team has approved BackgroundService
workers for long-running background tasks within the GP Products domain.

**Decision:** Implement as a .NET BackgroundService hosted within the existing
GP Products API rather than as a standalone Lambda function.

**Rationale:**
- BackgroundService is already an established pattern in the EMIS-X codebase
- Lambda would require a separate deployment pipeline and infrastructure ticket
- The job does not need to scale independently — appointment volumes per practice
  are bounded and predictable
- Alternatives considered: Hangfire (adds external dependency), AWS EventBridge
  (adds cross-service coupling), Lambda (adds infra overhead without benefit)

**Consequences:**
- The BackgroundService must be resilient to host restarts (idempotent processing)
- Monitoring requires OTEL spans on the job execution — not just HTTP request tracing
- If job volume grows beyond a single host's capacity, this decision requires revisiting
```

---

## DB Schema Standards

Every table introduced in P04 must:
- Have a Flyway migration file (`Vnn__description.sql`)
- Use `uuid_generate_v4()` for UUIDs (not `gen_random_uuid()`)
- Use singular table names (`notification_record`, not `notification_records`)
- Have a `{table_name}_uuid` primary key column
- Use `TIMESTAMPTZ` for all timestamps (never `TIMESTAMP`)
- Have explicit indexes for all foreign keys and query paths

Every EF entity configuration must:
- Have an explicit `IEntityTypeConfiguration<T>` class
- Specify `ToTable("snake_case_table_name")`
- Specify `HasColumnName("snake_case_column_name")` for every property
- Match the Flyway migration exactly

---

## Extension: When Context Graph Lands (Plan KG)

When the Context Graph is live, P03 will have access to every architectural decision made across all EMIS-X increments. Before proposing a new integration pattern, the agent will:
- Retrieve similar integration patterns from previous increments
- Surface any known failure modes from the historical decision log
- Flag if the proposed pattern conflicts with an established EMIS-X standard

ADRs will become cross-increment institutional knowledge, not just per-project records. The graph compounds every sprint.

---

## Checklist Before Approving P03/P04

- [ ] Every interface in the REQ file's Component Interfaces section is specified as an OpenAPI contract
- [ ] Every table in the REQ file's DB Schema section has a Flyway migration and an EF entity configuration
- [ ] Every ADR has context, decision, rationale, and consequences
- [ ] All OTEL spans required by the REQ file are specified
- [ ] Any architectural discoveries that affect the REQ file have been raised as CHANGE records and approved
- [ ] The architecture satisfies every AC in the REQ file

When all boxes are checked: approve and proceed to P05.

---

*Genesis AI Training — Module 3 v1.0 | July 2026*
*Next update: when Context Graph (Plan KG) lands*
