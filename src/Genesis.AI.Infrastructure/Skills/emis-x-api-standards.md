---
name: emis-x-api-standards
description: >
  Use this skill when generating, reviewing, or auditing JSON:API-compliant
  API controllers, resource endpoints, HTTP conventions, versioning, error
  responses, Emis.JsonApi service registration, resource DTO models,
  pagination, exception converters, or enum serialisation. Covers the
  code-generation guardrails API-001 through API-011 and API-016. For
  API design decisions (relationships, query parameters, ETags, Swagger),
  see the companion skill emis-x-api-resource-design.
metadata:
  version: 1.2.0
  applyTo:
    - emis-x-api
    - requirements
---

# EMIS-X API Standards Guardrails

Guardrails for building JSON:API-compliant API layers in EMIS-X microservices. Apply during code generation and code review.

**Target versions:** ASP.NET Core 10.0, `Emis.JsonApi` (internal NuGet package), Swashbuckle 9.0.x.

## Guardrails Index

| Guardrail | Name                              | Severity |
| --------- | --------------------------------- | -------- |
| API-001   | JSON:API Content Type             | High     |
| API-002   | Resource Naming                   | High     |
| API-003   | Resource Identifiers              | High     |
| API-004   | Accept Header Versioning          | Medium   |
| API-005   | Error Response Structure          | High     |
| API-006   | EMIS-Request-Id Propagation       | High     |
| API-007   | HTTP Methods & Status Codes       | High     |
| API-008   | Emis.JsonApi Service Registration | High     |
| API-009   | JSON:API Resource Models          | High     |
| API-010   | JSON:API Exception Converter      | High     |
| API-011   | JSON:API Pagination               | Medium   |
| API-016   | Enum Serialisation                | High     |

> **See also:** `emis-x-api-resource-design` skill for API-012 (Swagger),
> API-013 (Relationships), API-014 (ETags), API-015 (Query Parameters),
> plus resource modelling, filtering syntax, and sorting conventions.

---

## API-001: JSON:API Content Type

**Type:** Guardrail

**Requirement:** All API endpoints must produce and consume `application/vnd.api+json`. Use the `Emis.JsonApi` NuGet package. All response bodies must conform to the JSON:API specification.

**Severity:** High

**Exceptions:**
- Health check endpoints (`/healthz/*`) and Swagger endpoints may use standard content types.
- Internal pipeline re-execution controllers (e.g. `StatusCodeController` reached only via `UseStatusCodePagesWithReExecute`) are exempt — they are never reachable by external clients and are typically marked `[ApiExplorerSettings(IgnoreApi = true)]`.
- When the service does **not** register JSON:API support via `AddJsonApi()` in its DI configuration, the rules are:
  - `[Produces]` is required at controller class level.
  - `[Consumes]` is required at controller class level **or** on every `[HttpPost]`/`[HttpPut]`/`[HttpPatch]` action individually. GET-only controllers do not require `[Consumes]`.
  - `[Consumes]` is a **hard requirement** on payload actions even when a custom `IInputFormatter` already enforces the content type at runtime — it is required for correct Swagger request body documentation. Without it, Swashbuckle defaults to `application/json` which will produce incorrect OpenAPI specs and misleading Swagger UI behaviour.
  - The specific content type value is not enforced — use the actual type(s) the controller produces and consumes.

✅ **Good:**

```csharp
[ApiController]
[Route("[controller]")]
[Produces("application/vnd.api+json")]
[Consumes("application/vnd.api+json")]
public class GreetingsController(IMediator mediator, IMapper mapper) : ControllerBase
{ ... }
```

Response body:

```json
{
  "data": {
    "type": "ern:emis:greetings",
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "attributes": {
      "name": "John",
      "message": "Hello John!",
      "createdAt": "2026-01-29T10:30:00Z"
    },
    "links": { "self": "/greetings/3fa85f64-5717-4562-b3fc-2c963f66afa6" }
  }
}
```

❌ **Bad:**

```csharp
// Missing JSON:API content type attributes
[ApiController]
[Route("[controller]")]
public class GreetingsController : ControllerBase { ... }

// Non-JSON:API response
return Ok(new { name = "John", message = "Hello" });
```

---

## API-002: Resource Naming

**Type:** Guardrail

**Requirement:**

- For standard EMIS-X API endpoints: Resource URIs must use plural nouns, lowercase, hyphen-separated. No underscores, no abbreviations. Use hierarchy via `/` for sub-resources.
- For FHIR endpoints (implementing HL7 FHIR): Resource URIs and HTTP verbs must use PascalCase as required by the HL7/Firely FHIR specification. Do not convert FHIR resource names or verbs to lowercase.

**Severity:** High

**Exceptions:**

- FHIR endpoints only: PascalCase is required for resource and operation names to comply with HL7/Firely standards. This exception applies only to endpoints implementing FHIR).

✅ **Good (Standard):**

```
/organisations
/person-organisations
/clinical-events
/organisations/{id}/locations
/organisations/{id}/locations/{locationId}/contacts
```

✅ **Good (FHIR):**

```
/fhir/Patient
/fhir/Observation
/fhir/MedicationRequest
```

❌ **Bad:**

```
/organisation              # Singular
/clinical_events           # Underscore
/ClinicalEvents            # PascalCase (unless FHIR)
/orgs                      # Abbreviation
/orgs/{id}/getLocations    # Verb in URI
/fhir/patient              # Lowercase FHIR resource (should be PascalCase)
/fhir/medicationrequest    # Lowercase FHIR resource (should be PascalCase)
```

---

## API-003: Resource Identifiers

**Type:** Guardrail

**Requirement:** Use UUID or ERN (EMIS Resource Name) for all resource identifiers. Never expose auto-increment integer IDs. If a legacy integer ID is required, wrap it in an ERN. Clients must treat `id` as an opaque string.

**Severity:** High

**Exceptions:** None.

✅ **Good:**

```json
{
  "data": {
    "type": "ern:emis:greetings",
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "attributes": { ... }
  }
}
```

Legacy integer wrapped in ERN:

```json
{ "id": "ern:emis:ew:activity:235" }
```

❌ **Bad:**

```json
{
  "data": {
    "type": "greeting",
    "id": 42,
    "attributes": { ... }
  }
}
```

---

## API-004: Accept Header Versioning

**Type:** Guardrail

**Requirement:** Use Accept header-based versioning, not URL-based versioning. Controllers must declare `[ApiVersion]` and actions must use `[MapToApiVersion]`.

**Severity:** Medium

**Exceptions:** None.

✅ **Good:**

```
Accept: application/vnd.api+json; version=1
```

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("[controller]")]
[Produces("application/vnd.api+json")]
[Consumes("application/vnd.api+json")]
public class GreetingsController(IMediator mediator, IMapper mapper) : ControllerBase
{
    [HttpGet("{name}")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetGreeting(string name, CancellationToken cancellationToken)
    { ... }
}
```

❌ **Bad:**

```
/api/v1/greetings          # URL-based versioning
/api/v2/greetings
```

```csharp
// No [ApiVersion] or [MapToApiVersion]
[Route("api/v1/[controller]")]
public class GreetingsController : ControllerBase { ... }
```

---

## API-005: Error Response Structure

**Type:** Guardrail

**Requirement:** All error responses must follow the JSON:API error format with `id`, `status`, `code`, `title`, and optionally `detail`, `source`, and `meta`. The `code` field must use ERN format. Errors must be returned as an array.

**Severity:** High

**Exceptions:** None.

✅ **Good:**

```json
{
  "errors": [
    {
      "id": "07e699fd-2c2d-438d-891a-dcf4a4889eb1",
      "status": "400",
      "code": "ern:emis:person:validation:field-required",
      "title": "Field is required",
      "detail": "The 'name' field is required",
      "source": { "pointer": "/data/attributes/name" }
    }
  ]
}
```

| Key      | Required | Description                                                            |
| -------- | -------- | ---------------------------------------------------------------------- |
| `id`     | ✓        | Unique error instance ID (from logging or UUID)                        |
| `status` | ✓        | HTTP status as string                                                  |
| `code`   | ✓        | ERN error code (static, documented)                                    |
| `title`  | ✓        | Short summary (does not change per occurrence)                         |
| `detail` |          | Specific explanation for this occurrence                               |
| `source` |          | `pointer` (JSON path) or `parameter` (query param)                     |
| `meta`   |          | Variable data: `{ "detailVariables": ["minValue=7", "maxValue=120"] }` |

❌ **Bad:**

```json
{ "error": "Name is required" }
```

```json
{ "message": "Bad Request", "statusCode": 400 }
```

---

## API-006: EMIS-Request-Id Propagation

**Type:** Guardrail

**Requirement:** Every request must have an `EMIS-Request-Id` header (UUID). If not provided by the client, generate one. Propagate it to all downstream service calls, events, and log entries for distributed tracing.

**Severity:** High

**Exceptions:** None.

✅ **Good:**

```csharp
public class EmisRequestIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("EMIS-Request-Id", out var requestId)
            || string.IsNullOrWhiteSpace(requestId))
        {
            requestId = Guid.NewGuid().ToString();
            context.Request.Headers["EMIS-Request-Id"] = requestId;
        }

        context.Response.Headers["EMIS-Request-Id"] = requestId;
        // Store in scoped service for downstream propagation
        var correlationContext = context.RequestServices.GetRequiredService<ICorrelationContext>();
        correlationContext.RequestId = requestId!;

        await next(context);
    }
}
```

❌ **Bad:**

```csharp
// No request ID propagation — tracing is impossible
[HttpGet("{name}")]
public async Task<IActionResult> GetGreeting(string name, CancellationToken cancellationToken)
{
    var result = await mediator.Send(new GetGreetingQuery(name), cancellationToken);
    return Ok(result); // No EMIS-Request-Id in response
}
```

---

## API-007: HTTP Methods & Status Codes

**Type:** Guardrail

**Requirement:** Use correct HTTP methods and status codes as defined below. Use `PATCH` not `PUT` for partial updates (JSON:API does not support PUT). POST/PATCH must return the updated resource representation.

**Severity:** High

**Exceptions:**

- `StatusCodeController` (internal error/status handler) is ignored by this rule. This controller is used for ASP.NET Core status code re-execution and does not represent a JSON:API resource endpoint. Violations in this controller are not reported.

### HTTP Methods

| Method   | Usage              | Response                           |
| -------- | ------------------ | ---------------------------------- |
| `GET`    | Retrieve resources | 200 + resource; NO request body    |
| `POST`   | Create resource    | 201 + resource + `Location` header |
| `PATCH`  | Partial update     | 200 + updated resource             |
| `DELETE` | Remove resource    | 204 No Content                     |

### Status Codes

| Code    | When to Use                                           |
| ------- | ----------------------------------------------------- |
| **200** | Successful GET/PATCH                                  |
| **201** | POST created resource (include `Location` header)     |
| **204** | Successful DELETE, no body                            |
| **304** | GET with `if-none-match`, resource unchanged          |
| **400** | Validation errors (client can fix and retry)          |
| **401** | Authentication failed                                 |
| **403** | Authenticated but not authorised                      |
| **404** | Single resource not found (NOT for empty collections) |
| **409** | ETag mismatch or constraint violation                 |
| **412** | Precondition failed (`if-match` header)               |
| **415** | Wrong `Content-Type`                                  |
| **5xx** | Server errors (never expose implementation details)   |

✅ **Good:**

```csharp
[HttpPost]
[Authorize(Policy = AuthorisationPolicies.GreetingCreate)]
public async Task<IActionResult> CreateGreeting(
    [FromBody] Document document, CancellationToken cancellationToken)
{
    var command = mapper.Map<CreateGreetingCommand>(document);
    var result = await mediator.Send(command, cancellationToken);
    var response = mapper.Map<Document>(result);
    return CreatedAtAction(nameof(GetGreeting), new { name = result.Name }, response);
}
```

❌ **Bad:**

```csharp
// PUT instead of PATCH, returning 200 for creation
[HttpPut("{id}")]
public async Task<IActionResult> UpdateGreeting(Guid id, [FromBody] Document document)
{
    // ...
    return Ok(result); // Should be PATCH, and POST should return 201
}
```

---

## API-008: Emis.JsonApi Service Registration

**Type:** Guardrail

**Requirement:** JSON:API services must be registered using the `Emis.JsonApi` package in the correct order. A custom `IExceptionConverter` must be registered via `AddJsonApiExceptionConverter<T>()` **before** calling `AddJsonApi()`. The `RequestHeaderValidation` must have `ValidHosts` populated — the application will fail to start if it is null or empty. `UseJsonApi()` must be called in the middleware pipeline before `UseRouting()`.

**Severity:** High

**Exceptions:** None.

✅ **Good:**

```csharp
// 1. Exception converter registered BEFORE AddJsonApi
builder.Services.AddJsonApiExceptionConverter<JsonApiExceptionConverter>();

// 2. AddJsonApi with ValidHosts populated
var mvcCoreBuilder = builder.Services.AddMvcCore();
builder.Services.AddJsonApi(
    new RequestHeaderValidation { ValidHosts = ["localhost", "api.example.com"] },
    resources =>
    {
        resources.Add<UserResponseDto>("ern:emis:users", "/users", false);
        resources.Add<RoleResponseDto>("ern:emis:roles", "/roles", false);
    },
    mvcCoreBuilder,
    enableContentTypeValidation: true,
    _ => { });

// 3. Middleware — UseJsonApi before UseRouting
app.UseJsonApi();
app.UseRouting();
```

❌ **Bad:**

```csharp
// Missing IExceptionConverter — app will crash at runtime
builder.Services.AddJsonApi(
    new RequestHeaderValidation(),  // ValidHosts is null — app fails on startup
    resources => { /* ... */ },
    mvcCoreBuilder);

// ExceptionConverter registered AFTER AddJsonApi — not available during DI validation
builder.Services.AddJsonApiExceptionConverter<JsonApiExceptionConverter>();
```

---

## API-009: JSON:API Resource Models

**Type:** Guardrail

**Requirement:** All JSON:API resource DTOs must inherit from `Identifiable<TId>` (or implement `IIdentifiable`). Resource types must be registered via `resources.Add<T>()` in the `AddJsonApi()` callback using ERN format for the `publicName` (e.g., `"ern:emis:users"`). Relationship properties must use the `[HasSingleRelationship]` or `[HasManyRelationship]` annotations **and** use `internal get` to prevent them appearing in the `attributes` section. Properties with a relationship attribute that use `public get` (or omit an access modifier on the getter) will be serialised into both `attributes` and `relationships`, causing duplicate data.

**Severity:** High

**Exceptions:** None.

✅ **Good:**

```csharp
public sealed class UserResponseDto : Identifiable<Guid>
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("createdAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? CreatedAt { get; set; }

    [HasSingleRelationship(CanInclude = true)]
    public OrganisationResponseDto Organisation { internal get; set; }

    [HasManyRelationship(CanInclude = true)]
    public IEnumerable<RoleResponseDto> Roles { internal get; set; }

    [Meta]
    public string TotalCount { get; set; }
}
```

Registration:

```csharp
resources.Add<UserResponseDto>("ern:emis:users", "/users", false);
```

❌ **Bad:**

```csharp
// Not inheriting from Identifiable — package cannot serialise
public class UserResponseDto { public Guid Id { get; set; } }

// Relationship without annotation — appears in attributes, not relationships
public class OrderResponseDto : Identifiable<Guid>
{
    public CustomerResponseDto Customer { get; set; }  // Missing [HasSingleRelationship]
}

// Relationship with public get — appears in BOTH attributes AND relationships
[HasSingleRelationship(CanInclude = true)]
public OrganisationResponseDto Organisation { get; set; }  // Must be { internal get; set; }

// Non-ERN resource type name
resources.Add<UserResponseDto>("users", "/users", false);  // Should be "ern:emis:users"
```

---

## API-010: JSON:API Exception Converter

**Type:** Guardrail

**Requirement:** Every service using `Emis.JsonApi` must implement `IExceptionConverter` from `JsonApi.Resources` to convert domain exceptions into JSON:API `Error` objects. The converter must handle all domain exception types, mapping them to appropriate HTTP status codes. The converter is registered as a **singleton** — it must be stateless.

**Severity:** High

**Exceptions:** None.

✅ **Good:**

```csharp
public class JsonApiExceptionConverter : IExceptionConverter
{
    public IEnumerable<Error> Convert(Exception exception)
    {
        if (exception is NotFoundException notFound)
        {
            yield return new Error
            {
                Title = "Resource not found",
                Detail = notFound.Message,
                HttpStatusCode = HttpStatusCode.NotFound,
            };
        }

        if (exception is DomainValidationException validation)
        {
            yield return new Error
            {
                Title = "Validation failed",
                Detail = validation.Message,
                HttpStatusCode = HttpStatusCode.UnprocessableEntity,
            };
        }
    }
}
```

❌ **Bad:**

```csharp
// Injecting scoped dependencies — converter is singleton
public class BadExceptionConverter(IUserRepository repo) : IExceptionConverter { /* ... */ }
// Not implementing IExceptionConverter — relies on generic 500 fallback
```

### Exception Handling Priority

The package handles exceptions in this order: (1) `JsonApiException` — its `Errors` property is returned directly, (2) Custom `IExceptionConverter` — `Convert()` is called, (3) Fallback — generic 500 error. Built-in `JsonApiException` types (422 body parse, 400 query/pagination/host) are handled automatically — do not re-handle them in your converter.

---

## API-011: JSON:API Pagination

**Type:** Guardrail

**Requirement:** Collection endpoints (HTTP GET actions returning collections) must use `PaginationFilter` with the `[Pagination]` attribute from `Emis.JsonApi`. Use `PaginatedList<TItem, TFilter>` to return paginated results with automatic link generation. Use `[FromQueryFilter]` for JSON:API filter parameters.

**Severity:** Medium

**Exceptions:** Endpoints returning a fixed, small set of resources (e.g., configuration lookups) may omit pagination.

✅ **Good:**

```csharp
[HttpGet]
public async Task<IActionResult> GetUsers(
    [FromQuery, Pagination(MaxPageSize = 100, DefaultPageSize = 20)] PaginationFilter filter,
    [FromQueryFilter("status")] string status,
    CancellationToken cancellationToken)
{
    var result = await mediator.Send(
        new GetUsersQuery(filter, status), cancellationToken);
    // result is PaginatedList<UserDto, PaginationFilter>
    // Package auto-generates first/last/prev/next links
    return Ok(result);
}
```

❌ **Bad:**

```csharp
// Custom pagination instead of PaginationFilter
[HttpGet]
public async Task<IActionResult> GetUsers(
    [FromQuery] int page, [FromQuery] int pageSize)
{
    // Manual pagination — no JSON:API links generated
}

// No pagination on collection endpoint
[HttpGet]
public async Task<IActionResult> GetAllUsers(CancellationToken cancellationToken)
{
    var all = await mediator.Send(new GetAllUsersQuery(), cancellationToken);
    return Ok(all);  // Returns unbounded collection
}
```

> **Detailed `Emis.JsonApi` package reference** (resource annotations, serialisation document model, `JsonApiOptions`, Swashbuckle integration) is available in `references/emis-jsonapi-package.md`.

---

## API-016: Enum Serialisation

**Type:** Guardrail

**Requirement:** Enum properties on JSON:API resource DTOs (classes inheriting from `Identifiable<T>`) must be serialised as **lowercase, hyphen-separated strings** (e.g., `primary-address`, `work-address`). Enum properties must have a `[JsonConverter(typeof(JsonStringEnumConverter))]` attribute (or equivalent converter) to ensure string serialisation. Enum values are defined in the **domain schema**, not the API schema — the client maps enum values to localised display text.

**Severity:** High

**Exceptions:** None.

**Versioning impact:**

- **Adding** an enum value → minor version bump (evaluate consumer impact)
- **Removing or renaming** an enum value → major version bump (breaking change)

✅ **Good:**

```csharp
public sealed class ContactResponseDto : Identifiable<Guid>
{
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter<ContactType>))]
    public ContactType Type { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}
```

❌ **Bad:**

```csharp
// No JsonConverter — enum serialises as integer (0, 1, 2)
public sealed class ContactResponseDto : Identifiable<Guid>
{
    public ContactType Type { get; set; }  // Missing [JsonConverter]
}
```

---

## Critical Reminders

- Every response body must be JSON:API — content type `application/vnd.api+json`, data wrapped in `{ "data": ... }`, errors in `{ "errors": [...] }`. Use `Emis.JsonApi` for serialisation, never hand-roll (API-001, API-005)
- Relationship properties on DTOs must have `[HasSingleRelationship]` or `[HasManyRelationship]` **and** use `internal get` — public getters cause silent duplication between `attributes` and `relationships` (API-009)
- Resource types follow the ERN pattern (`ern:emis:{domain}`) and URIs use plural, lowercase, hyphen-separated nouns — no underscores or abbreviations (API-002, API-003)
- Enum properties on DTOs must have `[JsonConverter(typeof(JsonStringEnumConverter))]` — without it, enums serialise as integers instead of lowercase-hyphen strings (API-016)
- `AddJsonApiExceptionConverter<T>()` before `AddJsonApi()`, `UseJsonApi()` before `UseRouting()` — order matters (API-008)
- JSON:API uses `PATCH` for updates, not `PUT` — the spec explicitly does not support PUT (API-007)
- Empty collections return 200 with `{ "data": [] }`, not 404 — a 404 means the endpoint itself does not exist
- `RequestHeaderValidation.ValidHosts` cannot be null or empty — fails at runtime, not compile time
