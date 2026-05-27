---
name: emis-x-api-observability
description: >
  Use this skill when generating, reviewing, or auditing Dockerfiles,
  logging configuration, exception handling, or health check endpoints —
  even when the user does not mention "observability" directly. Covers
  OBS-001 through OBS-004.
metadata:
  version: 1.2.0
  applyTo:
    - emis-x-api
    - requirements
---

# EMIS-X Observability Guardrails

Apply these guardrails during code generation and code review. All generated code **must** satisfy every applicable guardrail.

## Architecture Overview

EMIS-X microservices use a layered observability approach:

| Layer | Responsibility | Mechanism |
|-------|---------------|-----------|
| **APM Agent** | Distributed tracing, request metrics, correlation, topology | Dynatrace OneAgent embedded in Docker image via `LD_PRELOAD` |
| **Log Shipping** | Container stdout → APM platform | Fluent Bit sidecar (configured at infrastructure level, not in application code) |
| **Structured Logging** | Explicit error/warning output in APM-compatible format | Serilog with custom `ITextFormatter`, environment-gated activation |
| **Exception Routing** | Centralised exception → log decision | Global MVC exception filter registered via DI |

> **Key principle:** Because APM auto-instrumentation handles request-level telemetry (response times, throughput, error rates, distributed traces, correlation IDs), application-level Serilog logging is reserved for **explicit error and warning messages only**. Do not duplicate request logging, correlation ID injection, or request metrics — the APM agent provides these automatically.

## Guardrails Index

| Guardrail | Name | Severity |
|-----------|------|----------|
| OBS-001 | Dockerfile APM Agent Integration | High |
| OBS-002 | Centralised Serilog Configuration | High |
| OBS-003 | Centralised Exception Logging Filter | High |
| OBS-004 | Health Check Probes | High |

---

## OBS-001: Dockerfile APM Agent Integration `High`

**Type:** Guardrail

**Requirement:** Every service Dockerfile must embed the Dynatrace OneAgent so that automatic distributed tracing, request metrics, and code-level observability are available in all deployed environments. The agent is loaded via `LD_PRELOAD` at runtime — no application code changes are required. The OneAgent image **must** use a pinned version tag for build reproducibility.

**Severity:** High

**Exceptions:** Debug Dockerfiles (`Debug.Dockerfile`, `*.debug.Dockerfile`) used only for local development are exempt.

### Required Dockerfile Elements

1. **Copy OneAgent binaries** into the final runtime image from a Dynatrace source with a **pinned version tag**:
   ```dockerfile
   COPY --from=public.ecr.aws/dynatrace/dynatrace-codemodules:1.329.67.20260112-133153-dotnet / /
   ```
   The image can be pulled from any registry (public ECR, private tenant, JFrog mirror) — the guardrail only requires that the source contains `dynatrace` and uses a version-pinned tag (not a mutable tag like `:dotnet`).

2. **Preload the agent** so it instruments the .NET runtime:
   ```dockerfile
   ENV LD_PRELOAD /opt/dynatrace/oneagent/agent/lib64/liboneagentproc.so
   ```

✅ **Good:**

```dockerfile
FROM centraluk.jfrog.io/glb-docker-vir/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM centraluk.jfrog.io/glb-docker-vir/dotnet/sdk:10.0 AS build
# ... build steps ...

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=public.ecr.aws/dynatrace/dynatrace-codemodules:1.329.67.20260112-133153-dotnet / /
ENV LD_PRELOAD=/opt/dynatrace/oneagent/agent/lib64/liboneagentproc.so
ENTRYPOINT ["dotnet", "MyService.Api.dll"]
```

❌ **Bad:**

```dockerfile
# ❌ Missing OneAgent entirely — no APM instrumentation
FROM centraluk.jfrog.io/glb-docker-vir/dotnet/aspnet:10.0 AS base
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyService.Api.dll"]
```

```dockerfile
# ❌ OneAgent copied but LD_PRELOAD not set — agent will not activate
COPY --from=public.ecr.aws/dynatrace/dynatrace-codemodules:1.329.67-dotnet / /
ENTRYPOINT ["dotnet", "MyService.Api.dll"]
```

```dockerfile
# ❌ Mutable tag — builds are not reproducible, tomorrow could pull a different agent
FROM ${DYNATRACE_ENV_URL}/linux/oneagent-codemodules:dotnet AS dynatrace
COPY --from=dynatrace / /
ENV LD_PRELOAD /opt/dynatrace/oneagent/agent/lib64/liboneagentproc.so
```

---

## OBS-002: Centralised Serilog Configuration `High`

**Type:** Guardrail

**Requirement:** All services must configure Serilog through a **shared extension method** in the Core or Crosscutting project. The Serilog configuration must be environment-gated (only activated when running in AWS), use a custom `ITextFormatter` that outputs in the APM platform's expected format, and enrich log entries with release metadata from standard environment variables.

**Severity:** High

**Exceptions:** None.

### Requirements

1. **Environment-gated activation** — Serilog must only configure its custom formatter when `HOSTING_ENVIRONMENT == "AWS"`. Outside AWS (local development), the default `Microsoft.Extensions.Logging` console provider handles output.

2. **APM-compatible formatter** — A custom `ITextFormatter` implementation (e.g., `DynatraceTextFormatter`) must format log output as JSON that the APM platform can parse and correlate with traces.

3. **Release metadata enrichment** — Log entries must be enriched with:
   - `DT_RELEASE_STAGE` → environment name (e.g., `production`, `staging`)
   - `DT_RELEASE_PRODUCT` → product/service name
   - `DT_RELEASE_VERSION` → deployment version/ID

4. **Shared extension method** — Configuration must live in a shared project (Core or Crosscutting), not duplicated per service.

5. **Program.cs registration** — Every service `Program.cs` must call the shared Serilog configuration extension.

✅ **Good:**

**Extension method (Core project):**

```csharp
public static class LoggingConfigurationExtensions
{
    public static void ConfigureSerilog(this ConfigureHostBuilder builder)
    {
        if (Environment.GetEnvironmentVariable("HOSTING_ENVIRONMENT") != "AWS")
            return;

        builder.UseSerilog(
            (context, services, configuration) =>
                configuration
                    .MinimumLevel.Is(LogEventLevel.Error)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("environment", Environment.GetEnvironmentVariable("DT_RELEASE_STAGE"))
                    .Enrich.WithProperty("product", Environment.GetEnvironmentVariable("DT_RELEASE_PRODUCT"))
                    .Enrich.WithProperty("version", Environment.GetEnvironmentVariable("DT_RELEASE_VERSION"))
                    .WriteTo.Console(
                        formatter: new DynatraceTextFormatter()));
    }
}
```

**Program.cs:**

```csharp
builder.Host.ConfigureSerilog();
```

❌ **Bad:**

```csharp
// ❌ No environment gate — Serilog APM formatter active locally
builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .WriteTo.Console(formatter: new DynatraceTextFormatter()));
```

```csharp
// ❌ Missing release metadata enrichment
builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .Enrich.FromLogContext()
        .WriteTo.Console(formatter: new DynatraceTextFormatter()));
```

```csharp
// ❌ Serilog configured inline in Program.cs instead of shared extension
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .Enrich.WithProperty("environment", Environment.GetEnvironmentVariable("DT_RELEASE_STAGE"))
        .WriteTo.Console(formatter: new DynatraceTextFormatter()));
```

---

## OBS-003: Centralised Exception Logging Filter `High`

**Type:** Guardrail

**Requirement:** Every service must have a centralised exception logging filter registered as a **global MVC filter** via dependency injection. The filter must route exceptions to the logger with appropriate detail levels — specifically, exceptions that may contain serialised request body data (e.g., `JsonApiException`) must **not** have their details logged to prevent PII leakage.

**Severity:** High

**Exceptions:** The `JsonApiException` handling requirement is only enforced when the service registers JSON:API support via `AddJsonApi()` in its DI configuration (e.g., `Program.cs`). Services that do not use JSON:API are not required to handle `JsonApiException` and will not be flagged for its absence.

### Requirements

1. **Exception filter class** — A class implementing `IExceptionFilter` (or `IAsyncExceptionFilter`) must exist in a shared project.

2. **Global MVC registration** — The filter must be registered globally via `options.Filters.AddService<>()` in the MVC configuration, not applied per-controller.

3. **PII-safe exception logging** — Exceptions that could contain serialised request bodies (e.g., `JsonApiException`) must be logged **without** the exception object to prevent PII from request payloads leaking into logs:
   ```csharp
   // ✅ Log message only — no exception details
   _logger.LogError("An unhandled JSON API exception has occurred");

   // ❌ Exception details may contain serialised request body with PII
   _logger.LogError(context.Exception, "An unhandled JSON API exception has occurred");
   ```

4. **Cancellation rethrow** — `OperationCanceledException` and `TaskCanceledException` must be rethrown to allow the cancellation middleware to handle them.

5. **Full logging for genuine errors** — All other unhandled exceptions must be logged with the full exception object for diagnostic purposes.

✅ **Good:**

```csharp
public class ExceptionLoggingFilter(ILogger<ExceptionLoggingFilter> logger) : IExceptionLoggingFilter
{
    private readonly ILogger<ExceptionLoggingFilter> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is JsonApiException)
        {
            _logger.LogError("An unhandled JSON API exception has occurred");
        }
        else if (context.Exception is OperationCanceledException or TaskCanceledException)
        {
            throw context.Exception;
        }
        else
        {
            _logger.LogError(context.Exception, "Unhandled Exception");
        }
    }
}
```

**DI registration:**

```csharp
services.AddScoped<IExceptionLoggingFilter, ExceptionLoggingFilter>();

services.AddMvcCore(options => options.Filters.AddService<IExceptionLoggingFilter>());
```

❌ **Bad:**

```csharp
// ❌ No exception filter — exceptions handled ad-hoc in controllers
[ApiController]
public class UsersController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        try { /* ... */ }
        catch (Exception ex) { _logger.LogError(ex, "Error"); }
    }
}
```

```csharp
// ❌ Logging JsonApiException with full details — PII may leak
public void OnException(ExceptionContext context)
{
    _logger.LogError(context.Exception, "Unhandled Exception: {Message}", context.Exception.Message);
}
```

```csharp
// ❌ Filter applied per-controller, not globally
[ServiceFilter(typeof(ExceptionLoggingFilter))]
public class UsersController : ControllerBase { }
```

---

## OBS-004: Health Check Probes `High`

**Type:** Guardrail

**Requirement:** Every API service must register ASP.NET Core health checks and expose both a **liveness** and **readiness** probe endpoint using the [ASP.NET Core Health Checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks) middleware.

- **Liveness** (`/health` or `/healthz`) — confirms the container is running and hasn't crashed. Used by ALBs/ingress controllers. Must **not** require authentication. Must **not** be exposed to the internet.
- **Readiness** (`/health/ready` or `/healthz/ready`) — confirms the container is ready to accept requests and critical dependencies are reachable. Must be exposed to the internet. Must require a valid Users solution token (any valid token, no specific scope). Should check critical dependencies (database, dependent APIs) but must **not** create circular health check references.

**Severity:** High

**Exceptions:** Background worker services or non-HTTP services that do not serve traffic are exempt. Utility/action projects (e.g., one-off scripts in `Utilities/Actions/`) are also exempt.

**Checks:**
1. At least one C# file calls `AddHealthChecks()` (or an extension method wrapping it)
2. At least one C# file calls `MapHealthChecks()` (or an extension method wrapping it)
3. A liveness path is mapped — path containing `/health` or `/healthz` (but not `/health/ready` or `/healthz/ready`)
4. A readiness path is mapped — path containing `/health/ready` or `/healthz/ready`

### Responses

**Liveness** (`/health` or `/healthz`):
| Status | Code | Body |
|--------|------|------|
| Healthy | 200 | `Healthy` |
| Unhealthy | 503 | `Unhealthy` |

**Readiness** (`/health/ready` or `/healthz/ready`):
| Status | Code | Body |
|--------|------|------|
| Healthy | 200 | `Healthy` |
| Degraded | 200 | `Degraded` |
| Unhealthy | 503 | `Unhealthy` |

### Required Pattern

```csharp
// Service registration — add dependency checks for readiness
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgresql");

// Liveness — no dependency checks, no authentication
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false
});

// Readiness — all registered checks, requires authentication
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true
}).RequireAuthorization("Authenticated");
```

✅ **Good:**

```csharp
// Extension method pattern (recommended)
public static class HealthCheckExtensions
{
    public static IServiceCollection AddApplicationHealthChecks(
        this IServiceCollection services, string connectionString)
    {
        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "postgresql");
        return services;
    }

    public static WebApplication MapApplicationHealthChecks(
        this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => false
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = _ => true
        }).RequireAuthorization("Authenticated");

        return app;
    }
}

// Program.cs
builder.Services.AddApplicationHealthChecks(connectionString);
var app = builder.Build();
app.MapApplicationHealthChecks();
```

```csharp
// Kubernetes-style paths (also accepted)
app.MapHealthChecks("/healthz", new HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
{
    Predicate = _ => true
}).RequireAuthorization("Authenticated");
```

❌ **Bad:**

```csharp
// ❌ No health checks configured
var app = builder.Build();
app.MapControllers();
app.Run();
```

```csharp
// ❌ Registration without mapping
builder.Services.AddHealthChecks();
var app = builder.Build();
app.MapControllers();
// Missing MapHealthChecks()
```

```csharp
// ❌ Only liveness, no readiness probe
app.MapHealthChecks("/health");
// Missing /health/ready
```

```csharp
// ❌ Circular health check reference
// Service A checks Service B's /health/ready
// Service B checks Service A's /health/ready — DO NOT DO THIS
```

---

## Gotchas

- Serilog configuration only activates when `HOSTING_ENVIRONMENT == "AWS"`. Locally, the default `Microsoft.Extensions.Logging` console provider handles output. If you see no structured JSON logs in local dev, that is expected — do not add Serilog console output for local environments.
- The Dynatrace OneAgent tag `:dotnet` is a **mutable tag** that silently updates. Always pin to a specific version like `:1.329.67.20260112-133153-dotnet`. The guardrail checker will flag mutable tags.
- The liveness probe (`/health`) must use `Predicate = _ => false` to skip all dependency checks. If you include database checks in liveness, a transient database outage will cause Kubernetes to **restart** the container instead of just marking it unready.
- `JsonApiException` must be logged **without** the exception object (`_logger.LogError("message")`, not `_logger.LogError(exception, "message")`). The exception details may contain serialised request body data with PII.
- The exception logging filter must be registered as a **global MVC filter** via `options.Filters.AddService<>()`, not applied per-controller with `[ServiceFilter]`. Per-controller registration is easy to forget on new controllers.
