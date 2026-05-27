---
name: emis-x-api-security
description: >
  Use this skill when generating, reviewing, or auditing API code that
  touches authorisation, database queries, logging, configuration,
  middleware pipelines, or response headers — even when the user does
  not mention "security" directly. Covers SEC-002 through SEC-006
  (SEC-001 removed — merged into AUTH-004 in emis-x-api-auth).
metadata:
  version: 1.2.0
  applyTo:
    - emis-x-api
    - requirements
---

# EMIS-X API Security Guardrails

Apply these guardrails during code generation and code review. All generated code **must** satisfy every applicable guardrail.

**Target versions:** ASP.NET Core 10.0, Entity Framework Core 10.0.

## Guardrails Index

| Guardrail | Name                     | Severity |
| --------- | ------------------------ | -------- |
| SEC-002   | SQL Injection Prevention | Critical |
| SEC-003   | Sensitive Data Logging   | High     |
| SEC-004   | Secrets in Code          | Critical |
| SEC-005   | Security Response Headers | High    |
| SEC-006   | TLS Certificate Validation | Critical |

---

## SEC-002: SQL Injection Prevention

**Type:** Guardrail

**Requirement:** All database queries must use EF Core LINQ or other ORM abstractions. Never use raw SQL, string concatenation, or string interpolation to build queries. Raw SQL methods (`FromSqlRaw`, `FromSqlInterpolated`, `ExecuteSqlRaw`) must not appear in application code.

**Severity:** Critical

**Exceptions:** None — no exceptions allowed.

✅ **Good:**

```csharp
// EF Core LINQ — safe by default, no raw SQL
var greeting = await context.Greetings
    .FirstOrDefaultAsync(greeting => greeting.Name == name, cancellationToken);
```

❌ **Bad:**

```csharp
// String interpolation in raw SQL
var sql = $"SELECT * FROM greetings WHERE name = '{name}'";

// String concatenation
var sql = "SELECT * FROM greetings WHERE name = '" + name + "'";

// FromSqlRaw with interpolation — NOT safe (see Gotchas)
context.Greetings.FromSqlRaw($"SELECT * FROM greetings WHERE name = '{name}'");

// FromSqlInterpolated — safe from injection but still raw SQL, violates ORM-only rule
context.Greetings.FromSqlInterpolated($"SELECT * FROM greetings WHERE name = {name}");
```

---

## SEC-003: Sensitive Data Logging

**Type:** Guardrail

**Requirement:** Never log sensitive data including passwords, tokens, API keys, full JWT tokens, NHS numbers, or other PII. Log only identifiers (e.g., userERN) and operational data. Use structured logging with named placeholders.

**Severity:** High

**Exceptions:** Hashed or masked values are acceptable for debugging purposes.

✅ **Good:**

```csharp
_logger.LogInformation("Greeting created for user {UserErn}", userErn);
_logger.LogWarning("Authentication failed for request {RequestId}", requestId);
_logger.LogDebug("Processing greeting {GreetingId} for organisation {OrgErn}", greetingId, orgErn);
```

❌ **Bad:**

```csharp
// Logging password
_logger.LogInformation("Login attempt: {Email}, password: {Password}", email, password);

// Logging full JWT token
_logger.LogDebug("Auth token: {Token}", Request.Headers["Authorization"]);

// Logging NHS number
_logger.LogInformation("Patient {NhsNumber} record accessed", nhsNumber);

// Dumping full request body (may contain PII)
_logger.LogDebug("Request body: {Body}", JsonSerializer.Serialize(requestBody));
```

---

## SEC-004: Secrets in Code

**Type:** Guardrail

**Requirement:** No secrets, API keys, passwords, or connection strings with credentials may be hardcoded in source code. All sensitive configuration must come from environment variables, Azure Key Vault, or user-secrets (local dev only).

**Severity:** Critical

**Exceptions:** `docker-compose*.yml` files with local development defaults, `*.example` files, test fixtures using `MockTokenGenerator`, C# object initialisers where the `Password` property value is a dotted property access chain (e.g. `Password = request.Password`), and field/variable declarations where `Password` appears as a substring of the identifier name (e.g. `_identityPassword = RequiredEnv(...)`), provided the value originates from environment variables or configuration binding.

✅ **Good:**

```csharp
// Configuration from environment
var connectionString = configuration.GetConnectionString("DefaultConnection");
var apiKey = configuration["ExternalService:ApiKey"];

// appsettings.json with non-sensitive defaults only
{
  "Jwt": {
    "Authority": "https://login.microsoftonline.com/{tenantId}/v2.0",
    "Audience": "api://{clientId}"
  }
}
```

```env
# .env file (gitignored) for local development
POSTGRES_PASSWORD=localdev
AUTH_CLIENT_SECRET=dev-secret
```

❌ **Bad:**

```csharp
// Hardcoded API key
var apiKey = "sk-12345abcdef67890";

// Connection string with password in source
var conn = "Host=db;Database=app;User=admin;Password=P@ssw0rd!";

// Secrets in appsettings.json (committed to git)
{
  "ExternalService": {
    "ApiKey": "real-production-key-here"
  }
}
```

---

## SEC-005: Security Response Headers

**Type:** Guardrail

**Requirement:** Every API must return mandatory security headers on all responses, suppress the Kestrel `Server` header, and correctly configure CORS middleware when cross-origin requests are needed.

**Severity:** High

**Required headers:**

| Header | Value | Purpose |
|--------|-------|---------|
| `Cache-Control` | `no-store` | Prevents caching of API responses containing sensitive data |
| `Content-Security-Policy` | `default-src 'self'; frame-ancestors 'none';` | Prevents XSS and clickjacking |
| `Referrer-Policy` | `no-referrer` | Prevents leaking URLs to external sites |
| `Strict-Transport-Security` | `max-age=63072000; includeSubDomains; preload` | Enforces HTTPS (2-year max-age per best practice) |
| `X-Content-Type-Options` | `nosniff` | Prevents MIME-type sniffing |
| `X-Frame-Options` | `deny` | Prevents framing (defence in depth alongside CSP) |
| `Permissions-Policy` | `geolocation=(self)` | Restricts browser feature access |

Additionally, the Kestrel `Server` header must be suppressed to avoid exposing server technology.

**CORS requirements:**

| Requirement | Detail |
|-------------|--------|
| Pipeline ordering | `UseCors()` must be registered **after** `UseRouting()` and **before** `UseAuthorization()` |
| Explicit origins | CORS policies must use `WithOrigins(...)` from configuration — never `AllowAnyOrigin()` |
| No wildcard + credentials | `AllowAnyOrigin()` must not be combined with `AllowCredentials()` (browsers block this) |
| Pipeline registration | If any controller uses `[EnableCors(...)]`, `UseCors()` must be registered in the middleware pipeline |
| Origin source | Allowed origins must come from environment variables or configuration — not hardcoded |

**Exceptions:** None — all APIs must set these headers and follow CORS rules.

✅ **Good:**

```csharp
// Middleware — registered before UseRouting()
public class ResponseHeadersMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task Invoke(HttpContext context)
    {
        context.Response.Headers.Append("Cache-Control", "no-store");
        context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; frame-ancestors 'none';");
        context.Response.Headers.Append("Referrer-Policy", "no-referrer");
        context.Response.Headers.Append("Strict-Transport-Security", "max-age=63072000; includeSubDomains; preload");
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "deny");
        context.Response.Headers.Append("Permissions-Policy", "geolocation=(self)");

        await _next(context);
    }
}

// Program.cs — suppress Server header and register middleware before routing
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// ...

webApplication.UseMiddleware<ResponseHeadersMiddleware>();

// Correct CORS ordering: after UseRouting, before UseAuthorization
webApplication.UseRouting();
webApplication.UseCors();
webApplication.UseAuthentication();
webApplication.UseAuthorization();
```

```csharp
// CORS configuration — origins from environment, explicit methods
var allowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")?
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? [];

services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", builder =>
    {
        if (allowedOrigins.Length > 0)
        {
            builder.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});
```

❌ **Bad:**

```csharp
// Middleware registered after routing — headers may not apply to all responses
webApplication.UseRouting();
webApplication.UseAuthorization();
webApplication.UseMiddleware<ResponseHeadersMiddleware>(); // Too late

// CORS after authorisation — too late for preflight requests
webApplication.UseRouting();
webApplication.UseAuthentication();
webApplication.UseAuthorization();
webApplication.UseCors();  // Wrong — must be before UseAuthorization

// Wildcard origins — insecure, allows any site
builder.AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod();

// Hardcoded origins — should come from configuration
builder.WithOrigins("https://app.example.com", "https://staging.example.com");
```

---

## SEC-006: TLS Certificate Validation `Critical`

**Type:** Guardrail

**Requirement:** TLS certificate validation must never be disabled or weakened. The following patterns are prohibited:
- `DangerousAcceptAnyServerCertificateValidator` (accepts any certificate including expired and self-signed)
- `ServerCertificateCustomValidationCallback` that always returns `true`
- `TrustServerCertificate=true` in connection strings
- `SslMode=Disable` or `SslMode=Allow` in PostgreSQL connection strings

**Severity:** Critical

**Exceptions:** None — TLS validation must be enforced in all environments including development.

✅ **Good:**

```csharp
// Connection string with proper TLS
"Host=mydb.example.com;Database=mydb;SslMode=VerifyFull"

// HttpClient with default certificate validation
builder.Services.AddHttpClient("api", client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
});
```

❌ **Bad:**

```csharp
// ❌ Accepts any certificate — MITM attacks possible
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

// ❌ Always returns true — bypasses all certificate checks
ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true

// ❌ Bypasses certificate validation in connection string
"Host=mydb;Database=mydb;TrustServerCertificate=true"

// ❌ Disables TLS entirely for PostgreSQL
"Host=mydb;Database=mydb;SslMode=Disable"
```

---

## Gotchas

- `FromSqlRaw` and `FromSqlInterpolated` are both raw SQL — even though `FromSqlInterpolated` parameterises values safely, raw SQL bypasses the ORM and should not appear in application code. Use EF Core LINQ instead.
- `ResponseHeadersMiddleware` must be registered **before** `UseRouting()` in Program.cs — registering it after means some responses (e.g., routing errors) bypass the middleware.
- `UseCors()` must be **after** `UseRouting()` and **before** `UseAuthorization()` — placing it anywhere else causes either `[EnableCors]` attribute failures or preflight request rejections.
- `AllowAnyOrigin()` combined with `AllowCredentials()` is silently blocked by browsers — it does not produce a server-side error but CORS requests will fail at runtime.
- The Kestrel `Server` header is enabled by default — suppress it explicitly with `options.AddServerHeader = false` in `ConfigureKestrel`.
- `DangerousAcceptAnyServerCertificateValidator` is commonly suggested for local development — it must never be used, even in dev. Use properly provisioned development certificates instead.
- `TrustServerCertificate=true` often appears in SQL Server connection string examples — it disables certificate validation entirely and must be removed. Use proper certificate trust chains.
- `SslMode=Disable` in PostgreSQL connection strings turns off TLS entirely. Use `SslMode=Require` at minimum, or `SslMode=VerifyFull` for production environments.
