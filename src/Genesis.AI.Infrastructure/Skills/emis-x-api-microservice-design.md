---
name: emis-x-api-microservice-design
description: >
  Use this skill when designing new microservices, evaluating service
  boundaries, splitting or combining services, or reviewing architecture
  decisions — even when the user does not mention "microservice" or
  "architecture" directly. Covers ARCH-001 through ARCH-005.
metadata:
  version: 1.2.0
  applyTo:
    - emis-x-api
    - requirements
---

# EMIS-X Microservice Design Guardrails

Apply these guardrails during code generation and code review. All generated code **must** satisfy every applicable guardrail.

**Target stack:** .NET 10.0, ASP.NET Core, MediatR 12.x, Docker (`mcr.microsoft.com/dotnet/aspnet:10.0`).

## Guardrails Index

| Guardrail | Name                       | Severity |
| --------- | -------------------------- | -------- |
| ARCH-001  | Capability-Based Design    | High     |
| ARCH-002  | Single Bounded Context     | High     |
| ARCH-003  | Exclusive Data Ownership   | Critical |
| ARCH-004  | Async Event Communication  | High     |
| ARCH-005  | Service Naming Convention  | Medium   |
| ARCH-006  | Layered Architecture       | High     |

---

## ARCH-001: Capability-Based Design

**Type:** Steer

**Requirement:** Design services around **business capabilities** (e.g., Referral Management, Prescription Management), not technical functions (PDF generation, email sending) or data domains. Each service must be right-sized for a single team (3–8 people).

**Severity:** High

**Exceptions:** Shared utility services (e.g., notifications) may exist but should be event-driven, not synchronously called.

**Evidence Required:** State which business capability the service implements, why this boundary was chosen, and confirm no technical-function services (e.g., "notification-service", "logging-service") were created.

### Service Sizing

| Size | Example | Issue |
|------|---------|-------|
| **Too Large** | Care Record API (multiple domains) | Low cohesion, tight coupling |
| **Just Right** | Referral Management | Cohesive, bounded, team-ownable |
| **Too Small** | Acute vs Repeat Prescriptions split | Coordination overhead |

**Split when:** Different change rates, different team owners, different scaling needs
**Combine when:** Changes together, single team owns it, similar performance profile

✅ **Good:** A "Referral Management" service that handles creating referrals, tracking status, eRS integration, and referral letters — all cohesive around the referral capability.

❌ **Bad:** A "PDF Service" that generates documents for every domain — technical function, not business capability.

---

## ARCH-002: Single Bounded Context

**Type:** Steer

**Requirement:** Each microservice must own exactly one Bounded Context. Domain concepts must not leak across service boundaries. Use published language (events/contracts) for cross-service communication.

**Severity:** High

**Exceptions:** None.

**Evidence Required:** Name the bounded context this service implements. Confirm that no domain concepts from other contexts leak into this service (e.g., no shared entities or direct model references across service boundaries).

✅ **Good:** The Prescription Management service defines its own `Patient` projection with only the fields it needs (name, NHS number), subscribed via events from Patient Demographics.

❌ **Bad:** The Prescription Management service directly references the Patient Demographics domain model or shares a `Patient` entity/table.

---

## ARCH-003: Exclusive Data Ownership

**Type:** Steer

**Requirement:** Each service must own its data exclusively. No shared databases. Other services must maintain **read-optimised projections** via event subscriptions. Never allow direct database access from another service.

**Severity:** Critical

**Exceptions:** None.

**Evidence Required:** Confirm the service owns its database exclusively. State how cross-service data is accessed (e.g., event subscriptions, read-optimised projections) and confirm no other service connects directly to this database.

✅ **Good:**

```
Referral Service          Patient Demographics Service
┌─────────────┐          ┌──────────────────┐
│ referrals   │          │ patients         │
│ patient_    │◄─events──│ (source of truth)│
│  projections│          │                  │
└─────────────┘          └──────────────────┘
  Own database             Own database
```

❌ **Bad:**

```
Referral Service ──SQL──► Shared Database ◄──SQL── Patient Service
```

---

## ARCH-004: Async Event Communication

**Type:** Guardrail

**Requirement:** Services must communicate via asynchronous events, never via synchronous service-to-service HTTP calls. Events must contain enough context for consumers to act without callbacks.

**Severity:** High

**Exceptions:** Gateway/BFF aggregation layers may make synchronous calls to backend services.

✅ **Good:**

```csharp
// Publisher: Referral Service
public class ReferralCreatedEvent : IntegrationEvent
{
    public Guid ReferralId { get; init; }
    public string PatientErn { get; init; }
    public string ReferralType { get; init; }
    public DateTime CreatedAt { get; init; }
}

// Consumer: Notifications Service subscribes to process notifications asynchronously
```

❌ **Bad:**

```csharp
// Synchronous service-to-service call
public class CreateReferralHandler(HttpClient patientClient) : IRequestHandler<...>
{
    public async Task<Guid> Handle(CreateReferralCommand request, CancellationToken ct)
    {
        // Direct HTTP call to another service — creates tight coupling
        var patient = await patientClient.GetAsync($"/patients/{request.PatientId}");
        // ...
    }
}
```

---

## ARCH-005: Service Naming Convention

**Type:** Guardrail

**Requirement:** Follow the EMIS-X naming convention for services and repositories.

**Severity:** Medium

**Exceptions:** None.

| Element | Pattern | Example |
|---------|---------|---------|
| Service name | `{Capability}.{SubCapability}.Api` | `Identity.Authentication.Api` |
| Repository name | `{capability}-{subcapability}-api` | `identity-authentication-api` |
| Namespace | `Emis.{Capability}.{SubCapability}` | `Emis.Identity.Authentication` |

✅ **Good:**

```
Solution:   Referral.Management.Api
Repository: referral-management-api
Namespace:  Emis.Referral.Management
```

❌ **Bad:**

```
Solution:   ReferralService          # Missing .Api suffix
Repository: ReferralService          # PascalCase, no kebab
Namespace:  ReferralService          # Missing Emis prefix
```

---

## ARCH-006: Layered Architecture

**Type:** Guardrail

**Requirement:** Every microservice must follow the standard layered architecture. Layer dependencies must flow inward only (Api → Domain ← Infrastructure). The Domain layer must have no infrastructure dependencies (only MediatR).

**Severity:** High

**Exceptions:** The Core project may be shared across services for base classes.

✅ **Good:**

```
src/
├── {ServiceName}.Api/              # HTTP presentation layer
├── {ServiceName}.Core/             # Shared abstractions (Entity, Middleware, Behaviours)
├── {ServiceName}.Domain/           # Business logic (Commands, Queries, Aggregates)
├── {ServiceName}.Infrastructure/   # Data access (EF Core, Repositories)
db/
└── migrations/                     # Flyway SQL migrations
tests/
├── {ServiceName}.Tests/            # Unit tests
├── {ServiceName}.IntegrationTests/ # Integration tests
├── {ServiceName}.ApiTests/         # E2E API tests
└── {ServiceName}.TestFramework/    # Shared test utilities
```

### Layer Dependencies

- **Core** → Base classes, interfaces, middleware, pipeline behaviours (no business logic)
- **Domain** → Commands, Queries, Aggregates, Interfaces (framework-agnostic, only MediatR)
- **Infrastructure** → EF Core DbContext, Repository implementations (references Domain)
- **Api** → Controllers, DTOs, DI configuration (references all layers)

❌ **Bad:**

```
src/
├── Controllers/        # Everything in one project
├── Models/
├── Data/
└── Services/
```

## Anti-Patterns

| Anti-Pattern | Problem | Correct Approach |
|-------------|---------|-----------------|
| CRUD-based services | No domain logic, anaemic model | Design around capabilities with rich domain |
| Overloaded services | Multiple bounded contexts | One Bounded Context per service |
| Shared database | Tight coupling, no autonomy | Own data, communicate via events |
| Service-to-service calls | Synchronous coupling, cascading failures | Use async eventing |

## Capability Examples

| Capability | Scope | Key Responsibilities |
|------------|-------|---------------------|
| **Referral Management** | Just right | Creating referrals, tracking status, eRS integration, referral letters |
| **Patient Demographics** | Just right | Patient identity, NHS number validation, address/contact management |
| **Appointment Scheduling** | Just right | Booking, rescheduling, cancellation |
| **Prescription Management** | Evaluate | May include acute, repeat, and fulfilment — assess if should split |
| **Consultation (Encounters)** | Orchestrator | Metadata, state, references to clinical activities |
| **Clinical Events** | Just right | Observations, SNOMED coding, clinical data capture |

## Capability Checklist

When designing a new service, verify:

- [ ] Aligned to business capability (not technical function)?
- [ ] Clear Bounded Context with distinct data ownership?
- [ ] Right-sized for single team ownership (3–8 people)?
- [ ] Events for cross-service communication (no sync calls)?
- [ ] Follows layered architecture pattern?
- [ ] Naming convention followed for solution, repo, and namespace?
- [ ] Registered in EMIS Developer Hub?


