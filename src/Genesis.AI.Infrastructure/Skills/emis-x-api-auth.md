---
name: emis-x-api-auth
description: >
  Use this skill when generating, reviewing, or auditing code that involves
  JWT authentication, authorisation policies, scope validation, or token
  handling — even when the user does not mention "auth" directly. Covers
  AUTH-001 through AUTH-007.
metadata:
  version: 1.2.0
  applyTo:
    - emis-x-api
---

# EMIS-X Authentication & Authorisation Guardrails

Apply these guardrails during code generation and code review. All generated code **must** satisfy every applicable guardrail.

**Target versions:** ASP.NET Core 10.0 JWT Bearer middleware (`Microsoft.AspNetCore.Authentication.JwtBearer` 10.0).

**Important context:** EMIS-X microservices do **not** issue JWT tokens. Tokens are issued by the **EMIS-X Users API** (backed by Azure AD B2C). Microservices are **consumers** of these tokens — they validate them and enforce scope-based authorisation policies.

## Guardrails Index

| Guardrail | Name                                    | Severity |
| --------- | --------------------------------------- | -------- |
| AUTH-001  | Token Types & the `authorizations` Claim | High     |
| AUTH-002  | Scope Format                             | High     |
| AUTH-003  | Authorisation Policy Definition          | High     |
| AUTH-004  | Controller Authorisation                 | High     |
| AUTH-005  | JWT Configuration                        | High     |
| AUTH-006  | Inbound Claims Mapping                   | Medium   |
| AUTH-007  | User & Application Restricted Support    | High     |

---

## AUTH-001: Token Types & the `authorizations` Claim

**Type:** Guardrail

**Requirement:** Microservices consume JWTs issued by the EMIS-X Users API. Tokens come in two forms — **User Restricted** (issued on behalf of a user) and **Application Restricted** (issued on behalf of an application/service). Both token types contain the `authorizations` claim, which is a **JSON array** of granted scopes. Services must validate tokens using the standard ASP.NET Core JWT Bearer middleware with OIDC discovery from the Authority URL.

**Severity:** High

**Exceptions:** None.

### The `authorizations` Claim

The `authorizations` claim is a **JSON array of strings** in the JWT payload:

```json
"authorizations": [
  "aieval-ass.write",
  "aieval-users.read",
  "aieval-cons.manage"
]
```

ASP.NET Core's JWT handler automatically maps each array element to a separate `Claim` object with the name `"authorizations"`. This means `RequireClaim("authorizations", "aieval-ass.write")` works correctly — it checks whether any claim named `"authorizations"` has that value.

### User Restricted Token

Issued when a user authenticates (Authorization Code + PKCE flow). Contains user context:

```json
{
  "aud": "d01a8de0-4fa8-49d8-9bcc-5666da887470",
  "iss": "https://identity.int.emishealthsolutions.com/{tenantId}/v2.0/",
  "sub": "f2612ca0-4be2-4eac-bb5c-381db1f9eec2",
  "userERN": "ern:emis:user:user:f2612ca0-4be2-4eac-bb5c-381db1f9eec2",
  "email": "luke.smith@emishealth.com",
  "givenName": "Luke",
  "familyName": "Smith",
  "orgERN": "ern:emis:org:org:d5753427-76ec-48e8-ab19-7f3308d5ca3d",
  "orgName": "BURNHAM SURGERY",
  "roleERNs": ["ern:emis:user:role:5809c68a-62d8-4030-9699-c77c59ce80a5"],
  "roleNames": ["Product 50002 role"],
  "authorizations": [
    "clinical-cr.read",
    "clinical-cr.write",
    "doc-app.read",
    "doc-app.create"
  ],
  "scp": "emisweb cdb ods emis-x",
  "ods": "F81126",
  "cdb": "50002"
}
```

### Application Restricted Token

Issued via Client Credentials flow (no user present). Contains application context only:

```json
{
  "aud": "71fd3a57-ae12-4ebf-bb0b-035685102335",
  "iss": "https://identity.emis-x.uk/{tenantId}/v2.0/",
  "sub": "dafb91a6-961c-478d-9c91-83f44fe41970",
  "appERN": "ern:emis:user:app:c211b4c0-e967-4bda-a875-fa05959deae0",
  "appName": "EMIS - Care Record",
  "orgERN": "ern:emis:org:org:5f1ffc31-aa85-463a-8683-1db544d95fec",
  "orgName": "Master",
  "authorizations": [
    "serg-endp.read"
  ]
}
```

**Key differences:**

| Claim | User Restricted | Application Restricted |
|-------|-----------------|----------------------|
| `userERN` | ✓ Present | ✗ Absent |
| `appERN` | ✗ Absent | ✓ Present |
| `email`, `givenName`, `familyName` | ✓ Present | ✗ Absent |
| `roleERNs`, `roleNames` | ✓ Present | ✗ Absent |
| `orgERN`, `orgName` | ✓ Present | ✓ Present |
| `authorizations` | ✓ Present | ✓ Present |

✅ **Good:**

```csharp
// Using RequireClaim to match scopes — works with both token types
policy.RequireClaim("authorizations", "aieval-ass.write");

// Checking for namespace prefix across all authorizations claims
policy.RequireAssertion(context =>
{
    var authorizations = context.User.FindAll("authorizations");
    return authorizations.Any(c => c.Value.StartsWith("aieval-"));
});
```

❌ **Bad:**

```csharp
// WRONG: Treating authorizations as a space-separated string
var authorizationsClaim = context.User.FindFirst("authorizations");
var authorizations = authorizationsClaim.Value.Split(' ');

// WRONG: Treating authorizations as a nested JSON object
var model = JsonSerializer.Deserialize<AuthorisationsModel>(authorizationsClaim);

// WRONG: Looking for scopes in standard OAuth claims
policy.RequireClaim("scp", "read");
policy.RequireClaim("scope", "greeting.read");

// WRONG: Assuming userERN is always present (Application Restricted tokens don't have it)
var userErn = context.User.FindFirstValue("userERN")
    ?? throw new UnauthorizedAccessException("userERN required");
```

---

## AUTH-002: Scope Format

**Type:** Guardrail

**Requirement:** Authorisation scopes must follow the format `{namespace}-{resource}.{action}`. The namespace is 3–12 lowercase alpha characters. The resource portion is 3–12 lowercase alpha characters optionally with dots, and typically includes the action.

**Severity:** High

**Exceptions:**

- `const string` values that begin with `http://` or `https://` are never valid OAuth scopes and are excluded from validation. This prevents false positives from FHIR identifier system URIs and other URL constants that happen to contain hyphens and dots (e.g. `https://fhir.nhs.uk/Id/nhs-number`).

### Scope Format

```
{namespace}-{resource}.{action}
```

**Regex:** `^([a-z]{3,12})-([a-z.]{3,12})$`

| Component | Rules | Examples |
|-----------|-------|---------|
| `namespace` | 3–12 lowercase chars; the module namespace (also used in ERNs) | `auth`, `clinical`, `aieval`, `audit` |
| `resource.action` | 3–12 lowercase chars with dots; resource type + action | `app.write`, `cr.read`, `ass.manage` |

### Examples

| Scope | Namespace | Resource.Action |
|-------|-----------|-----------------|
| `auth-app.write` | `auth` | `app.write` |
| `auth-role.create` | `auth` | `role.create` |
| `clinical-cr.read` | `clinical` | `cr.read` |
| `audit-recd.view` | `audit` | `recd.view` |
| `aieval-ass.write` | `aieval` | `ass.write` |
| `aieval-cons.manage` | `aieval` | `cons.manage` |
| `aieval-users.read` | `aieval` | `users.read` |

✅ **Good:**

```csharp
// Follows the {namespace}-{resource}.{action} pattern
public const string AssessmentWrite = "aieval-ass.write";
public const string ConsultationRead = "aieval-cons.read";
public const string UserManage = "aieval-users.manage";
```

❌ **Bad:**

```csharp
// Wrong formats
public const string Read = "read";                                // No namespace or resource
public const string GreetingRead = "greeting:read";               // Colon separator
public const string GreetingRead = "Greetings.Greeting.Read";     // PascalCase, wrong format
public const string GreetingRead = "greetings-greeting.read";     // Namespace too long (max 12)
```

---

## AUTH-003: Authorisation Policy Definition

**Type:** Guardrail

**Requirement:** Authorisation policies must be defined using the `AddAuthorisationPolicy` extension method pattern. This method adds a policy that requires the JWT Bearer authentication scheme, an authenticated user, and the `authorizations` claim containing the required scope value(s). Policies work identically for both User Restricted and Application Restricted tokens since both contain the `authorizations` claim.

**Severity:** High

**Exceptions:** None.

### Extension Method Pattern

```csharp
public static class AuthorisationPolicyExtensions
{
    /// <summary>
    /// Adds a policy requiring specific scope values in the "authorizations" claim.
    /// Works with both User Restricted and Application Restricted tokens.
    /// </summary>
    public static AuthorizationOptions AddAuthorisationPolicy(
        this AuthorizationOptions options, string name, params string[] claims)
    {
        options.AddPolicy(name, policy =>
        {
            policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
            policy.RequireAuthenticatedUser();
            policy.RequireClaim("authorizations", claims);
        });
        return options;
    }

    /// <summary>
    /// Adds a policy requiring authentication only (no specific scopes).
    /// Accepts any valid token regardless of type.
    /// </summary>
    public static AuthorizationOptions AddAuthenticationOnlyPolicy(
        this AuthorizationOptions options, string name)
    {
        options.AddPolicy(name, policy =>
        {
            policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
            policy.RequireAuthenticatedUser();
        });
        return options;
    }

    /// <summary>
    /// Adds a policy accepting any scope starting with the service namespace prefix.
    /// Uses FindAll to correctly handle the array-based authorizations claim.
    /// </summary>
    public static AuthorizationOptions AddAnyNamespaceAuthorisationPolicy(
        this AuthorizationOptions options, string name, string namespacePrefix)
    {
        options.AddPolicy(name, policy =>
        {
            policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context =>
            {
                var authorizations = context.User.FindAll("authorizations");
                return authorizations.Any(c => c.Value.StartsWith(namespacePrefix));
            });
        });
        return options;
    }
}
```

✅ **Good:**

```csharp
// Registration in Program.cs
builder.Services.AddAuthorization(options =>
{
    options.AddAuthorisationPolicy("AssessmentWrite", "aieval-ass.write");
    options.AddAuthorisationPolicy("AssessmentManage", "aieval-ass.manage");
    options.AddAuthorisationPolicy("ConsultationRead", "aieval-cons.read");
    options.AddAuthorisationPolicy("ConsultationManage", "aieval-cons.manage");
    options.AddAuthorisationPolicy("UserRead", "aieval-users.read");
    options.AddAuthorisationPolicy("UserManage", "aieval-users.manage");
});
```

❌ **Bad:**

```csharp
// Inline policy definitions without the extension method
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanRead", policy =>
        policy.RequireClaim("scope", "read"));    // Wrong claim name

    options.AddPolicy("CanWrite", policy =>
        policy.RequireRole("admin"));             // Role-based, not scope-based
});
```

---

## AUTH-004: Controller Authorisation

**Type:** Guardrail

**Requirement:** Every API endpoint must enforce an authorisation policy via `[Authorize(Policy = ...)]`. No endpoint may be left unprotected without an explicit `[AllowAnonymous]` attribute and documented justification. Both per-action and controller-level authorisation are valid — use controller-level when all endpoints share the same policy, per-action when different endpoints require different policies. Base-class authorisation attributes are inherited — a `[Authorize(Policy = "...")]` on a `BaseController` covers all derived controllers.

**Severity:** High

**Exceptions:** `/swagger`, `/healthz/live`, `/healthz/ready` endpoints may use `[AllowAnonymous]`. Controllers that cannot guarantee an authenticated principal at the point of execution (e.g. error re-execution middleware targets, diagnostic info pages) may place `[AllowAnonymous]` at the **class level** — this is treated as explicitly opted-out for all actions in that controller. A justification comment must accompany the attribute.

### Per-Action Authorisation (mixed policies)

✅ **Good:**

```csharp
[ApiController]
[Route("[controller]")]
public class ConsultationsController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "ConsultationRead")]
    public async Task<IActionResult> GetConsultations() { ... }

    [HttpPost]
    [Authorize(Policy = "ConsultationManage")]
    public async Task<IActionResult> CreateConsultation([FromBody] ...dto) { ... }

    [HttpPut("{id}")]
    [Authorize(Policy = "ConsultationManage")]
    public async Task<IActionResult> UpdateConsultation(Guid id, [FromBody] ...dto) { ... }
}
```

### Controller-Level Authorisation (uniform policy)

✅ **Good:**

```csharp
// All endpoints share the same policy
[ApiController]
[Route("[controller]")]
[Authorize(Policy = "DatasetManage")]
public class DatasetsController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetDatasets() { ... }

    [HttpPost]
    public async Task<IActionResult> CreateDataset([FromBody] ...dto) { ... }
}
```

### Base-Class Authorisation (inherited policy)

✅ **Good:**

```csharp
// BaseController carries the policy — derived controllers inherit it
[Authorize(Policy = "OrgErn")]
public abstract class BaseController : ControllerBase { }

[Route("users")]
public class UsersController : BaseController
{
    [HttpGet]
    public IActionResult Get() => Ok(); // Covered by BaseController's policy
}
```

### Anonymous Endpoints

✅ **Good:**

```csharp
// Action-level: explicit anonymous with justification
[HttpGet("healthz/live")]
[AllowAnonymous] // Health probes must be unauthenticated for orchestration
public IActionResult LivenessProbe() => Ok();

// Class-level: entire controller is anonymous with justification
// Re-execution runs after the pipeline has partially unwound — no auth principal is guaranteed.
[AllowAnonymous]
[Route("[controller]")]
public class StatusCodeController : Controller
{
    [Route("{statusCode}")]
    [HttpGet, HttpPost, HttpPut, HttpDelete]
    public IActionResult HandleError(int statusCode) => Ok();
}
```

❌ **Bad:**

```csharp
// No authorisation at all — endpoint is open
[HttpGet]
public async Task<IActionResult> GetConsultations() { ... }

// Controller-level auth but too coarse — all actions share one policy
[Authorize]
public class GreetingsController : ControllerBase
{
    [HttpGet("{name}")] // Same policy for read and delete?
    public async Task<IActionResult> GetGreeting(...) { ... }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGreeting(...) { ... }
}
```

---

## AUTH-005: JWT Configuration

**Type:** Guardrail

**Requirement:** JWT Bearer authentication must be configured in `Program.cs` using `Authentication:Authority` and `Authentication:Audience` from configuration. Authority points to the EMIS-X Users API's Azure AD B2C OIDC discovery endpoint. Validation must be enabled for issuer, audience, lifetime, and signing key. Set `MapInboundClaims = false` to preserve original claim names.

**Severity:** High

**Exceptions:** In local development, Authority and Audience may be empty to allow running without auth. Validation should be conditionally enabled based on whether values are configured.

✅ **Good:**

```csharp
// Program.cs
var jwtAuthority = builder.Configuration["Authentication:Authority"];
var jwtAudience = builder.Configuration["Authentication:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        if (!string.IsNullOrEmpty(jwtAuthority))
            options.Authority = jwtAuthority;
        if (!string.IsNullOrEmpty(jwtAudience))
            options.Audience = jwtAudience;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrEmpty(jwtAuthority),
            ValidateAudience = !string.IsNullOrEmpty(jwtAudience),
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        // Preserve original JWT claim names — do not remap to Microsoft long-form URIs
        options.MapInboundClaims = false;
    });
```

```json
// appsettings.json — non-sensitive defaults (empty for local dev)
{
  "Authentication": {
    "Authority": "",
    "Audience": ""
  }
}

// appsettings.Development.json — dev environment values
{
  "Authentication": {
    "Authority": "https://identity.dev.emishealthsolutions.com/{tenantId}/v2.0/",
    "Audience": "{clientId}"
  }
}
```

Middleware ordering:
```csharp
app.UseAuthentication();
app.UseAuthorization();
```

❌ **Bad:**

```csharp
// Hardcoded authority and audience
options.Authority = "https://identity.dev.emishealthsolutions.com/abc123/v2.0/";
options.Audience = "8a9616a7-f3ba-4626-8659-84d9cf272e0c";

// Validation disabled
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = false,
    ValidateAudience = false,
    ValidateLifetime = false
};

// Missing MapInboundClaims = false — claim names get remapped
// "authorizations" becomes "http://schemas.microsoft.com/..." and policy matching breaks
```

---

## AUTH-006: Inbound Claims Mapping

**Type:** Guardrail

**Requirement:** Always set `MapInboundClaims = false` on JWT Bearer options. Without this, ASP.NET Core remaps JWT claim names (e.g., `sub` → `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier`), which breaks `RequireClaim("authorizations", ...)` policy matching.

**Severity:** Medium

**Exceptions:** None.

✅ **Good:**

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // ... authority, audience config ...

        // CRITICAL: Preserve original JWT claim names
        options.MapInboundClaims = false;
    });
```

❌ **Bad:**

```csharp
// MapInboundClaims defaults to true — claims get remapped
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = configuration["Authentication:Authority"];
        options.Audience = configuration["Authentication:Audience"];
        // MapInboundClaims not set — defaults to true!
        // "authorizations" claim is now inaccessible by its original name
    });
```

---

## AUTH-007: User & Application Restricted Support

**Type:** Guardrail

**Requirement:** EMIS-X API endpoints must accept **both** User Restricted and Application Restricted tokens by default. Authorisation decisions must be based solely on the `authorizations` claim (present in both token types), not on the presence of user-specific claims like `userERN`.

**Severity:** High

**Exceptions:**
- Endpoints that **always require user context** (e.g., issuing a prescription on behalf of a specific clinician) may restrict to User Restricted tokens only. Document this requirement explicitly.
- **Internal API-to-API endpoints** may restrict to Application Restricted tokens only. In this case, verify that `orgERN` matches the MASTER organisation to ensure only internal services can call.

### On-Behalf-Of (Service-to-Service with User Context)

When a service needs to call another service **on behalf of a user**, the calling service uses an **Application Restricted token** (which always has the MASTER `orgERN`: `ern:emis:org:org:5f1ffc31-aa85-463a-8683-1db544d95fec`) and passes the user's context via an `On-Behalf-Of` header. These two concepts are tied together — if an `On-Behalf-Of` header is present, the `orgERN` on the token **must** be the MASTER organisation. The receiving API should:

1. Verify the token is Application Restricted (`appERN` present)
2. Verify `orgERN` is the MASTER organisation (`ern:emis:org:org:5f1ffc31-aa85-463a-8683-1db544d95fec`)
3. Extract user context from the `On-Behalf-Of` header
4. Reject requests where `On-Behalf-Of` is present but `orgERN` is not MASTER



### Token Type Categories

| Category | Token Source | `userERN` | `appERN` | Use Case |
|----------|------------|-----------|----------|----------|
| **User Restricted** | Authorization Code + PKCE (user present) | ✓ | ✗ | ACP/frontend user interactions |
| **Application Restricted** | Client Credentials (no user) | ✗ | ✓ | Service-to-service, background tasks |

### De Facto: Accept Both

The `authorizations` claim is present in both token types. Policies based on `RequireClaim("authorizations", ...)` work transparently regardless of token type.

✅ **Good:**

```csharp
// This policy works for both token types — only checks authorizations
options.AddAuthorisationPolicy("ConsultationRead", "aieval-cons.read");

// Controller doesn't assume user context
[HttpGet]
[Authorize(Policy = "ConsultationRead")]
public async Task<IActionResult> GetConsultations()
{
    // Logic works regardless of whether caller is a user or service
    var consultations = await _context.Consultations.ToListAsync();
    return Ok(consultations);
}
```

❌ **Bad:**

```csharp
// WRONG: Assuming all callers are users
[HttpGet]
[Authorize(Policy = "ConsultationRead")]
public async Task<IActionResult> GetConsultations()
{
    // This fails for Application Restricted tokens!
    var userErn = User.FindFirstValue("userERN")
        ?? throw new UnauthorizedAccessException("User required");

    var consultations = await _context.Consultations
        .Where(c => c.AssessorErn == userErn)
        .ToListAsync();
    return Ok(consultations);
}
```

### When User Context Is Required

If an endpoint genuinely requires user context, check for `userERN` explicitly and return 403 for Application Restricted tokens:

```csharp
[HttpPost("prescriptions")]
[Authorize(Policy = "PrescriptionCreate")]
public async Task<IActionResult> IssuePrescription([FromBody] ...dto)
{
    var userErn = User.FindFirstValue("userERN");
    if (string.IsNullOrEmpty(userErn))
    {
        return Forbid(); // Application Restricted token — user context required
    }

    // Proceed with user context...
}
```

### When Only Application Restricted Is Allowed (Internal API-to-API)

For internal-only endpoints, verify the token is Application Restricted and from a trusted (MASTER) organisation:

```csharp
[HttpPost("internal/sync")]
[Authorize(Policy = "InternalSync")]
public async Task<IActionResult> SyncData([FromBody] ...dto)
{
    // Verify this is an Application Restricted token from MASTER org
    var appErn = User.FindFirstValue("appERN");
    var orgErn = User.FindFirstValue("orgERN");

    if (string.IsNullOrEmpty(appErn))
        return Forbid(); // Not an Application Restricted token

    if (orgErn != "ern:emis:org:org:5f1ffc31-aa85-463a-8683-1db544d95fec")
        return Forbid(); // Not from MASTER organisation — reject

    // Proceed...
}
```

---

## Gotchas

- The claim name is `authorizations` (with a **z**) — not `authorisations`. This is one of the rare cases where American spelling is correct, because it matches the JWT payload emitted by the Users API. Using British spelling in `RequireClaim` will silently match nothing.
- `MapInboundClaims` defaults to `true` in ASP.NET Core. If you forget to set it to `false`, the middleware silently renames JWT claims to long-form Microsoft URIs (`http://schemas.xmlsoap.org/...`), and `RequireClaim("authorizations", ...)` stops matching — with no error, just 403s.
- The `scp` claim in User Restricted tokens is **not** the authorisation scopes. It contains OAuth2 audience scopes (`emisweb cdb ods emis-x`) and must not be used for policy decisions. The scopes you care about are always in `authorizations`.
- `RequireClaim("authorizations", "scope-a", "scope-b")` means the caller needs **any one** of those scopes (OR logic), not all of them. For AND logic, chain multiple `RequireClaim` calls.

---

## Critical Reminders

- Scopes live in the `authorizations` claim (a JSON array), not in `scp` or `scope` — policies must use `RequireClaim("authorizations", ...)` to validate them
- Every controller action needs an `[Authorize(Policy = "...")]` attribute referencing a named policy — bare `[Authorize]` only proves the caller has a valid token, it does not check scopes
- Microservices never issue tokens — they consume JWTs from the EMIS-X Users API and must configure `Authority` + `Audience` via `AddJwtBearerAuthentication()`, not hand-rolled middleware
- Both User Restricted and Application Restricted tokens carry `authorizations` — do not assume a user is present unless you explicitly check for `userERN`

