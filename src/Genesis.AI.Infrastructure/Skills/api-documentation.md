# SKILL: api-documentation
# Phase: P04 Design — Phase 11

## API Documentation

**Purpose:** Design OpenAPI annotations and Swagger configuration for all endpoints.

### Required Swagger Annotations (API-012)

Every controller action must have:

```csharp
/// <summary>{Operation description}</summary>
[HttpPost("{id}")]
[Authorize(Policy = "{PolicyName}")]
[ProducesResponseType(typeof({ResourceType}), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
public async Task<IActionResult> {MethodName}(...)
```

### Swagger Request/Response Examples (API-013)

For each significant endpoint, design an example:

```csharp
[SwaggerRequestExample(typeof({RequestType}), typeof({RequestType}Example))]
[SwaggerResponseExample(StatusCodes.Status200OK, typeof({ResourceType}Example))]
```

### API Documentation Template

```markdown
### API Documentation

| Endpoint | Summary | Request example | Response example | Error codes |
|---------|---------|-----------------|-----------------|-------------|
| {METHOD} {path} | {Summary} | {DTO fields} | {Resource shape} | 400, 401, 403, 422 |
```

### Swagger XML Comments

Confirm that each DTO property has an XML doc comment for Swagger display:

```csharp
/// <summary>The unique identifier for the resource.</summary>
/// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
public required Guid Id { get; init; }
```
