# SKILL: error-handling-strategy
# Phase: P04 Design — Phase 6

## Error Handling Strategy

**Purpose:** Define error handling patterns, domain exceptions, and HTTP status code mappings.

### Questions

1. "Error handling pattern?" → Result<T> for expected failures, exceptions for unexpected
2. "What domain exceptions are needed?" → NotFoundException, ConflictException, ValidationException
3. "HTTP status code mapping?" → 404 NotFound, 409 Conflict, 400/422 Validation
4. "How are errors logged?" → Structured logging with correlation IDs (OBS-003)

### Result Pattern (Preferred for Domain Logic)

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public string Error { get; }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
}
```

### Domain Exception Pattern

```csharp
public sealed class {Entity}NotFoundException : NotFoundException
{
    public {Entity}NotFoundException(Guid id)
        : base($"{Entity} with ID '{id}' was not found.")
    {
    }
}
```

### HTTP Status Code Mapping

| Domain Exception | HTTP Status | JSON:API Error Title |
|-----------------|-------------|---------------------|
| NotFoundException | 404 | "Not Found" |
| ConflictException | 409 | "Conflict" |
| ValidationException | 422 | "Unprocessable Entity" |
| UnauthorizedException | 403 | "Forbidden" |

### Error Logging Rule (OBS-003)

All exceptions must be logged with a correlation ID. Never log PHI/PII in error messages.

### Validation

```
"Error handling for REQ-{NNN}:
- Pattern: {Result<T> / exceptions}
- Domain exceptions: {list}
- Status codes: {mapping}
- Logging: structured with correlation ID, no PII

Correct?"
```
