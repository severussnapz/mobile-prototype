---
name: emis-x-api-domain-driven-design
description: >
  Use this skill when generating, reviewing, or auditing domain logic,
  CQRS commands/queries, aggregates, entities, value objects, domain
  events, validation, or applying SOLID and coding conventions — even
  when the user does not mention "DDD" directly. Covers ENG-001
  through ENG-012.
metadata:
  version: 1.2.0
  applyTo:
    - emis-x-api
    - requirements
---

# EMIS-X Engineering Standards Guardrails

Apply these guardrails during code generation and code review. All generated code **must** satisfy every applicable guardrail.

**Target versions:** .NET 10.0, C# 13, MediatR 12.x, FluentValidation 11.x, AutoMapper 13.x.

## Guardrails Index

| Guardrail | Name                            | Severity |
| --------- | ------------------------------- | -------- |
| ENG-001   | British English                 | Medium   |
| ENG-002   | CQRS Separation                 | High     |
| ENG-003   | DDD Aggregates                  | High     |
| ENG-004   | Repository Interface Placement  | High     |
| ENG-005   | Dependency Inversion            | High     |
| ENG-006   | Async/Await Patterns            | High     |
| ENG-007   | FluentValidation                | Medium   |
| ENG-008   | Thin Controllers                | High     |
| ENG-009   | Method and Class Complexity     | Medium   |
| ENG-010   | Block Body Methods              | Medium   |
| ENG-011   | Descriptive Lambda Parameters   | Medium   |
| ENG-012   | Layer Knowledge Isolation        | High     |

---

## ENG-001: British English

**Type:** Guardrail

**Requirement:** String literals, comments, and documentation must use British English spelling. Code identifiers (class names, method names, properties, variables) may use either British or American English — American English is acceptable in code because .NET framework types use American spelling and forcing British creates inconsistency with the framework. Additionally, only use acronyms and abbreviations that are widely recognised within the domain (e.g., NHS, GP, API, HTTP, ERN, UUID, DTO, CQRS). Spell out names in full unless a well-known abbreviation exists — use `Organisation` not `Org`, `Patient` not `Pat`, `Configuration` not `Config`.

**Severity:** Medium

**Exceptions:**
- Short-form identifiers in LINQ expressions (e.g., `g` for greeting in a lambda) are acceptable.
- Files inside `Generated/` folders are exempt — machine-generated code (e.g. FHIR R4 builder extensions) is permitted to use American spelling.
- String literals that are URLs are exempt — HL7 FHIR defines standard resource URIs using American spelling (e.g. `"http://hl7.org/fhir/StructureDefinition/Organization"`, `"https://fhir.nhs.uk/Id/ods-organization-code"`). These are external standards that cannot be changed.
- String literals that are CSS property values are exempt — CSS property names such as `color` and `background-color` are standard technical vocabulary, not natural language.

✅ **Good (strings/comments):** `"Organisation"`, `"Authorisation required"`, `// Colour preference`, `/// <summary>Behaviour settings</summary>`

✅ **Good (code — both acceptable):** `OrganizationService` or `OrganisationService`, `Color` or `Colour`, `Serialize` or `Serialise`

✅ **Good (acronyms):** `NHSNumber`, `GPConnect`, `ApiResponse`, `UserDto`, `GetByERN`

❌ **Bad (strings):** `"Organization"`, `"Color preference"`, `"Authorization required"`

❌ **Bad (abbreviations):** `OrgId` (use `OrganisationId` or `OrganizationId`), `PatNo` (use `PatientNumber`), `Cfg` (use `Configuration`)

---

## ENG-002: CQRS Separation

**Type:** Guardrail

**Requirement:** Commands and queries must be strictly separated. Commands mutate state and return void or simple results. Queries retrieve data and never mutate state. Both must implement `IRequest<T>` (MediatR).

**Severity:** High

**Exceptions:** None.

✅ **Good:**

```csharp
// Command — mutates state
public record CreateGreetingCommand(string Name, string Message) : IRequest<Guid>;

public class CreateGreetingCommandHandler(IGreetingRepository repository)
    : IRequestHandler<CreateGreetingCommand, Guid>
{
    public async Task<Guid> Handle(CreateGreetingCommand request, CancellationToken cancellationToken)
    {
        var greeting = new Greeting(request.Name, request.Message);
        await repository.AddAsync(greeting, cancellationToken);
        return greeting.Id;
    }
}

// Query — reads only
public record GetGreetingQuery(string Name) : IRequest<Greeting?>;

public class GetGreetingQueryHandler(IGreetingRepository repository)
    : IRequestHandler<GetGreetingQuery, Greeting?>
{
    public async Task<Greeting?> Handle(GetGreetingQuery request, CancellationToken cancellationToken)
        => await repository.GetByNameAsync(request.Name, cancellationToken);
}
```

❌ **Bad:**

```csharp
// Query handler that also modifies data
public class GetGreetingQueryHandler(IGreetingRepository repository)
    : IRequestHandler<GetGreetingQuery, Greeting?>
{
    public async Task<Greeting?> Handle(GetGreetingQuery request, CancellationToken cancellationToken)
    {
        var greeting = await repository.GetByNameAsync(request.Name, cancellationToken);
        greeting.LastAccessedAt = DateTime.UtcNow; // Mutation in a query!
        await repository.UpdateAsync(greeting, cancellationToken);
        return greeting;
    }
}
```

---

## ENG-003: DDD Aggregates

**Type:** Guardrail

**Requirement:** Domain aggregates must inherit from `Entity` (or `Entity<TId>` for strongly-typed identifiers) and implement `IAggregateRoot`. Use private setters to protect invariants. Include a private parameterless constructor for EF Core. Raise domain events via `AddDomainEvent()`.

**Immutability:** Value objects, commands, queries, domain events, and result types must be **immutable**. Use `record` types for these. Only aggregates and entities may have mutable state (via private setters controlled by domain methods). Making these types immutable eliminates side effects and simplifies reasoning about domain logic.

**Domain folder naming:** Domain projects must **not** contain a folder named `Models/`. This name is generic and encourages anemic data structures instead of rich domain types. Use:
- `AggregatesModel/` — for aggregate roots, entities, and value objects
- `ReadModels/` or `Projections/` — for immutable read-only projections consumed by queries
- `Commands/`, `Queries/`, `Events/` — for CQRS types (already required by ENG-002)

**Non-aggregate immutability:** Types in Domain projects that are **not** inside `AggregatesModel/` and are not interfaces must be immutable. Properties must use `get; init;` or `{ get; }` — never `get; set;`. These types should be declared as `record` types. This prevents anemic CRUD entities from leaking into the domain layer.

**Severity:** High

**Exceptions:** None.

✅ **Good:**

```csharp
// Aggregate in AggregatesModel/ — private setters, domain methods
public class Greeting : Entity, IAggregateRoot
{
    public string Name { get; private set; }
    public string Message { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Greeting() { } // EF Core

    public Greeting(string name, string message, TimeProvider timeProvider)
    {
        Id = Guid.NewGuid();
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        CreatedAt = timeProvider.GetUtcNow().UtcDateTime;
        AddDomainEvent(new GreetingCreatedEvent(Id, name));
    }
}

// Read model in ReadModels/ — immutable record
public record GreetingReadModel(Guid Id, string Name, string Message, DateTime CreatedAt);

// Alternative read model with init properties
public record UserOrganisationRole
{
    public required Guid UserId { get; init; }
    public required string OrganisationName { get; init; }
    public required IReadOnlyList<string> Roles { get; init; }
}
```

✅ **Good folder structure:**

```
Domain/
  AggregatesModel/
    GreetingAggregate/
      Greeting.cs              # Entity + IAggregateRoot
      GreetingContent.cs       # ValueObject
  ReadModels/
    GreetingReadModel.cs       # Immutable record
  Commands/
    CreateGreetingCommand.cs   # Immutable record
  Interfaces/
    IGreetingRepository.cs     # Repository interface
```

❌ **Bad:**

```csharp
// No Entity/IAggregateRoot, public setters, no encapsulation
public class Greeting
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Message { get; set; }
}

// Mutable type outside AggregatesModel/ — anemic CRUD entity
public record UserInOrganisationRecord
{
    public Guid UserId { get; set; }
    public string OrganisationName { get; set; }
}
```

❌ **Bad folder structure:**

```
Domain/
  Models/                        # Prohibited — generic name, encourages anemic types
    UserInOrganisationRecord.cs  # Mutable get; set; properties
    LoginContext.cs
```

---

## ENG-004: Repository Interface Placement

**Type:** Guardrail

**Requirement:** Repository interfaces must be defined in the Domain layer. Implementations must be in the Infrastructure layer. Domain must never reference Infrastructure.

**Severity:** High

**Exceptions:** None.

✅ **Good:**

```
Domain/
  Interfaces/IGreetingRepository.cs    # Interface here
Infrastructure/
  Repositories/GreetingRepository.cs   # Implementation here
```

❌ **Bad:**

```
Infrastructure/
  IGreetingRepository.cs               # Interface in Infrastructure
  GreetingRepository.cs
```

---

## ENG-005: Dependency Inversion

**Type:** Guardrail

**Requirement:** All dependencies must be injected via constructor injection. Depend on abstractions (interfaces), never on concrete implementations. Use primary constructors (C# 13).

**Interface Segregation:** Keep interfaces focused on a single responsibility. A client should not be forced to depend on methods it does not use. If a type serves multiple contexts, define separate context-specific interfaces rather than one broad interface. This improves clarity, testability, and decoupling.

**Severity:** High

**Exceptions:** None.

✅ **Good:**

```csharp
public class CreateGreetingCommandHandler(IGreetingRepository repository)
    : IRequestHandler<CreateGreetingCommand, Guid> { ... }
```

❌ **Bad:**

```csharp
public class CreateGreetingCommandHandler : IRequestHandler<CreateGreetingCommand, Guid>
{
    private readonly GreetingRepository _repository = new GreetingRepository(); // Concrete + new
}
```

---

## ENG-006: Async/Await Patterns

**Type:** Guardrail

**Requirement:** All I/O operations must use async/await. All async methods must accept `CancellationToken`. Never use `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()` in hand-authored code.

**Severity:** High

**Exceptions:**

- **Auto-generated files** — files whose first 15 lines contain an `<auto-generated>` header comment (written by T4, Source Generators, NSwag, or similar tools) are exempt from hard violations. Findings in these files are reported as **warnings**, not violations. Fix the generator or template, not the generated output.
- **`?.Result` null-conditional access** — `?.Result` is treated as a property access (e.g., `ApiException<T>.Result`), not a blocking `Task.Result` call.
- **Top-level `Program.cs`** — `.GetAwaiter().GetResult()` is acceptable in top-level statements where async entry points are not available.

✅ **Good:**

```csharp
public async Task<Greeting?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    => await context.Greetings.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
```

✅ **Good (exception property access — not a blocking call):**

```csharp
// ApiException<T>.Result is a typed response property, not Task.Result
catch (ApiException<ProblemDetails> ex)
{
    var details = ex?.Result;
}
```

❌ **Bad:**

```csharp
// Blocking call
public Greeting? GetById(Guid id)
    => context.Greetings.FirstOrDefaultAsync(g => g.Id == id).Result;

// Missing CancellationToken
public async Task<Greeting?> GetByIdAsync(Guid id)
    => await context.Greetings.FirstOrDefaultAsync(g => g.Id == id);
```

---

## ENG-007: FluentValidation

**Type:** Guardrail

**Requirement:** All commands and queries requiring input validation must have a corresponding `AbstractValidator<T>` class. Validation logic must be separate from handler logic.

**Severity:** Medium

**Exceptions:** Queries with only primitive parameters (e.g., a single `Guid` or `string`) may omit a validator.

✅ **Good:**

```csharp
public class CreateGreetingCommandValidator : AbstractValidator<CreateGreetingCommand>
{
    public CreateGreetingCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(500);
    }
}
```

❌ **Bad:**

```csharp
// Validation inside the handler
public async Task<Guid> Handle(CreateGreetingCommand request, CancellationToken cancellationToken)
{
    if (string.IsNullOrEmpty(request.Name))
        throw new ArgumentException("Name is required");
    // ...
}
```

---

## ENG-008: Thin Controllers

**Type:** Guardrail

**Requirement:** Controllers must delegate all business logic to MediatR handlers. Controllers should only map between API models and domain commands/queries, then send via `IMediator`. Use AutoMapper for DTO mapping.

**Severity:** High

**Exceptions:** None.

✅ **Good:**

```csharp
[HttpPost]
public async Task<IActionResult> CreateGreeting([FromBody] Document document, CancellationToken cancellationToken)
{
    var command = _mapper.Map<CreateGreetingCommand>(document);
    var result = await _mediator.Send(command, cancellationToken);
    // Return JSON:API response
}
```

❌ **Bad:**

```csharp
[HttpPost]
public async Task<IActionResult> CreateGreeting([FromBody] Document document, CancellationToken cancellationToken)
{
    // Business logic in controller
    var greeting = new Greeting(document.Data.Attributes.Name, document.Data.Attributes.Message);
    await _context.Greetings.AddAsync(greeting, cancellationToken);
    await _context.SaveChangesAsync(cancellationToken);
    return Created($"/greetings/{greeting.Id}", greeting);
}
```

## ENG-009: Method and Class Complexity

**Type:** Guardrail

**Requirement:** Methods and classes must follow the Single Responsibility Principle. Large methods must be decomposed into smaller, focused units. Thresholds:

- **Method body:** Maximum **20 statements** (excluding braces, blank lines, and comments). Methods exceeding this are doing too much and must be refactored into smaller private methods or extracted into collaborating services.
- **Class file:** Maximum **200 lines** (excluding blank lines and comments). Classes exceeding this likely violate SRP and should be decomposed.

**Severity:** Medium

**Exceptions:**
- Auto-generated files (e.g. EF Core migrations, designer files)
- Entity type configuration classes (`IEntityTypeConfiguration<T>`) which are inherently declarative
- Test classes — test methods may be longer due to Arrange/Act/Assert structure
- Files containing a `// guardrail:skip=ENG-009` comment with justification

✅ **Good:**

```csharp
public async Task GetProfileDataAsync(ProfileDataRequestContext context)
{
    var userId = ParseSubjectId(context);
    if (userId is null) return;

    var user = await LookupUserAsync(userId.Value);
    if (user is null) return;

    var claims = BuildBasicClaims(user);
    await EnrichWithOrganisationClaims(context, userId.Value, claims);
    await EnrichWithPermissions(userId.Value, claims);
    await EnrichWithExternalIdentifiers(userId.Value, claims);

    context.IssuedClaims.AddRange(claims);
}
```

❌ **Bad:**

```csharp
public async Task GetProfileDataAsync(ProfileDataRequestContext context)
{
    // 170+ lines of user lookup, organisation resolution, role resolution,
    // NHS role lookup, product resolution, permission aggregation,
    // external identifier mapping, and claim construction all in one method.
    // This violates SRP — each concern should be a separate method.
}
```

---

## ENG-010: Block Body Methods

**Type:** Guardrail

**Requirement:** All methods must use block body syntax with braces `{ }`. Expression-bodied member syntax (`=>`) must not be used for methods. This ensures consistent readability and makes debugging easier (breakpoints can be set on individual statements).

**Severity:** Medium

**Exceptions:**
- Properties (getters/setters) may use expression-bodied syntax: `public string Name => _name;`
- Operator overloads and conversion operators
- Test classes
- Files containing a `// guardrail:skip=ENG-010` comment with justification

✅ **Good:**

```csharp
public async Task<ApplicationRecord?> GetByClientIdAsync(
    string clientId,
    CancellationToken cancellationToken = default)
{
    return await _context.Applications
        .AsNoTracking()
        .FirstOrDefaultAsync(application => application.ClientId == clientId, cancellationToken);
}
```

❌ **Bad:**

```csharp
public async Task<ApplicationRecord?> GetByClientIdAsync(
    string clientId,
    CancellationToken cancellationToken = default) =>
    await _context.Applications
        .AsNoTracking()
        .FirstOrDefaultAsync(a => a.ClientId == clientId, cancellationToken);
```

---

## ENG-011: Descriptive Lambda Parameters

**Type:** Guardrail

**Requirement:** Lambda parameters must use descriptive names that convey meaning. Single-letter parameter names are not permitted. The parameter name should reflect the type or role of the object being operated on.

**Severity:** Medium

**Exceptions:**
- Mathematical or coordinate lambdas where single letters are conventional (e.g. `(x, y) =>` for coordinates)
- Very short lambdas used with `Action<T>` where the type is obvious from immediate context (e.g. `options.Configure(o => o.Timeout = 30)` in DI registration)
- Test classes
- Files containing a `// guardrail:skip=ENG-011` comment with justification

✅ **Good:**

```csharp
.Where(scope => scope.ApplicationId == applicationId)
.FirstOrDefaultAsync(application => application.ClientId == clientId)
.Select(role => new RoleDto { Id = role.Id, Name = role.Name })
.Any(permission => permission.IsActive)
```

❌ **Bad:**

```csharp
.Where(s => s.ApplicationId == applicationId)
.FirstOrDefaultAsync(a => a.ClientId == clientId)
.Select(r => new RoleDto { Id = r.Id, Name = r.Name })
.Any(p => p.IsActive)
```

---

## ENG-012: Layer Knowledge Isolation

**Type:** Guardrail

**Requirement:** Code that knows about types from a lower layer belongs in that
lower layer. ENG-005 requires classes to depend on abstractions — this rule
extends that principle to the layer level. A file in a higher layer (core,
crosscutting, API) MUST NOT reference, import, or construct types from a lower
layer (infrastructure, SDK) that it does not own.

**Litmus test:** would this file still compile if you removed all lower-layer
project references and SDK packages? If not, the code is in the wrong layer —
move it down and expose it via an interface.

Common violations:

- Importing an SDK or infrastructure namespace in a higher layer
- Constructing a concrete infrastructure type in a DI lambda, factory, or
  helper in a higher layer
- A DI registration method that bundles unrelated concerns, forcing consumers
  to satisfy dependencies they do not use

The fix is always the same: move the infrastructure-aware code into the
infrastructure layer and expose it to higher layers via an interface.

**Severity:** High

**Exceptions:** None.

✅ **Good — higher layer only knows about abstractions:**

```csharp
// Crosscutting — compiles without any infrastructure reference
using Users.Core.Messaging;

public class EventReceiver(IMessageReceiver receiver)
```

❌ **Bad — higher layer knows about infrastructure types it doesn't need:**

```csharp
// Crosscutting — requires infrastructure reference to compile
using Users.Infrastructure.Hosting.AmazonWebServices.SQS;  // ✗

public class EventReceiver(SqsMessageReceiver receiver)  // ✗
```

---

## Additional Engineering Conventions

### Value Objects

Value objects are immutable and defined by their attributes rather than identity. They must inherit from `ValueObject`:

```csharp
public class GreetingContent : ValueObject
{
    public string Salutation { get; private set; }
    public string Body { get; private set; }

    public GreetingContent(string salutation, string body)
    {
        Salutation = salutation;
        Body = body;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Salutation;
        yield return Body;
    }
}
```

### Domain Events

Domain events represent something significant that happened in the domain:

- **Internal events:** Dispatched via MediatR when DbContext commits, for loose coupling within the service
- **Integration events:** Published to other services via the event-driven architecture
- Events must contain enough context for consumers to act without callbacks
- Version event schemas for cross-service compatibility

```csharp
public class GreetingCreatedEvent : Event
{
    public Guid GreetingId { get; }
    public string Name { get; }

    public GreetingCreatedEvent(Guid greetingId, string name)
    {
        GreetingId = greetingId;
        Name = name;
    }
}
```

### C# Conventions

- Enable nullable reference types (`<Nullable>enable</Nullable>`)
- Use records for immutable DTOs, commands, and queries
- Use file-scoped namespaces
- Use primary constructors (C# 13) for dependency injection
- Use expression-bodied members for simple properties/methods

### Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Classes | PascalCase | `GreetingService` |
| Interfaces | IPascalCase | `IGreetingRepository` |
| Methods | PascalCase | `GetByNameAsync` |
| Properties | PascalCase | `CreatedAt` |
| Private fields | _camelCase | `_repository` |
| Parameters | camelCase | `greetingName` |
| Async methods | Suffix with Async | `GetByIdAsync` |
---

## Gotchas

- A folder called `Models/` in a Domain project is a guardrail violation (ENG-003). Use `AggregatesModel/` for aggregates/entities, `ReadModels/` for read-only projections. The name "Models" encourages anemic data structures and agents will default to it unless told otherwise.
- Expression-bodied methods (`=>`) are banned for methods (ENG-010), even though C# promotes them and agents prefer them for one-liners. Properties may still use arrow syntax — the ban applies specifically to methods, to keep debugging and breakpoint placement straightforward.
- Single-letter lambda parameters (`x =>`, `s =>`, `e =>`) are banned (ENG-011). Use the entity name: `scope =>`, `user =>`, `greeting =>`. Agents tend to abbreviate aggressively in LINQ chains.
- `.Result` and `.Wait()` are banned (ENG-006) — but agents frequently generate them when adapting synchronous examples. Always use `await` and propagate `CancellationToken`.
- Read models outside `AggregatesModel/` must be immutable — `get; init;` or `{ get; }` only, never `get; set;`. Declare them as `record` types. Agents default to mutable properties unless explicitly told.
- Infrastructure types leaking into higher layers (ENG-012) — agents frequently `new` up infrastructure types inside DI lambdas in crosscutting code, or add `using Amazon.*` imports to files that should not know about infrastructure. If a file needs an infrastructure reference to compile, the code belongs in the infrastructure layer.

---

## Critical Reminders

- Commands mutate state, queries read state — never mix the two in a single handler (ENG-002)
- Aggregates inherit from `Entity` + `IAggregateRoot`, use private setters, and raise domain events via `AddDomainEvent()` — no public setters, no anaemic models (ENG-003)
- Controllers are thin dispatchers — map to a command/query, send via `IMediator`, return the result. No business logic in controllers (ENG-008)
- All methods use block body `{ }` syntax, never expression-bodied `=>` for methods; all lambda parameters use descriptive names, never single letters (ENG-010, ENG-011)
- British English in strings, comments, and documentation — `"Organisation"`, `"Authorisation required"` — American English is acceptable in code identifiers because .NET uses American spelling (ENG-001)
- Code that knows about infrastructure types belongs in the infrastructure layer — if a file in crosscutting or core needs an SDK import or infrastructure reference to compile, move the code down and expose it via an interface (ENG-012)
