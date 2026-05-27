---
name: emis-x-api-csharp-standards
description: >
  Use this skill when generating, reviewing, or auditing C# code in any
  EMIS-X API project — covering file organisation, class structure,
  constructor patterns, type safety, complexity, import hygiene, and dead
  code removal. Covers CS-001 through CS-017.
metadata:
  version: 1.2.0
  applyTo:
    - emis-x-api
---

# EMIS-X C# Coding Standards

C# language-level coding standards that apply to all EMIS-X API projects. These complement the domain-driven design guardrails (ENG prefix) and data access guardrails (DATA prefix) — this skill focuses on code structure, type safety, and language usage.

**Target versions:** C# 13, .NET 10.0

## Rules Index

| Rule   | Name                             | Type      | Severity |
| ------ | -------------------------------- | --------- | -------- |
| CS-001 | Single Type Per File             | Guardrail | High     |
| CS-002 | No Partial Classes               | Guardrail | High     |
| CS-003 | No Magic Numbers                 | Guardrail | Medium   |
| CS-004 | Global Usings                    | Steer     | Medium   |
| CS-005 | Explicit Private Readonly Fields | Guardrail | High     |
| CS-006 | Method Complexity                | Guardrail | Medium   |
| CS-007 | No Ref/Out Parameters            | Guardrail | Medium   |
| CS-008 | No Member Hiding                 | Guardrail | High     |
| CS-009 | No Regions                       | Guardrail | Medium   |
| CS-010 | Namespace-Folder Alignment       | Guardrail | High     |
| CS-011 | Explicit Access Modifiers        | Guardrail | Medium   |
| CS-012 | No Dynamic Keyword               | Guardrail | High     |
| CS-013 | Self-Documenting Code            | Steer     | Medium   |
| CS-014 | No Logging Level Guards          | Guardrail | Medium   |
| CS-015 | .NET Analysers Enabled           | Guardrail | High     |
| CS-016 | Dead Code Removal                | Steer     | High     |
| CS-017 | Inject TimeProvider              | Guardrail | High     |

---

## CS-001: Single Type Per File

**Type:** Guardrail

**Requirement:** Each `.cs` file must contain exactly one top-level type declaration (class, struct, record, interface, or enum). The file name must match the type name. Never place multiple classes, records, or interfaces in a single file.

**Severity:** High

**Exceptions:**

- Nested types (private inner classes within a parent type) are permitted as they belong to the enclosing type.
- Test projects may have test helper types in the same file as the test class when they are only used by that test.

✅ **Good:**

```
UserService.cs          → contains class UserService
IUserRepository.cs      → contains interface IUserRepository
UserStatus.cs           → contains enum UserStatus
UserCreatedEvent.cs     → contains record UserCreatedEvent
```

```csharp
// UserService.cs
namespace MyService.Domain.Services;

public class UserService
{
    // A private nested type is fine
    private sealed class UserCacheEntry
    {
        public Guid Id { get; init; }
        public DateTime CachedAt { get; init; }
    }
}
```

❌ **Bad:**

```csharp
// Models.cs — multiple types in one file
namespace MyService.Domain;

public class User { }
public class Organisation { }
public record UserDto(string Name);
public enum UserStatus { Active, Inactive }
```

---

## CS-002: No Partial Classes

**Type:** Guardrail

**Requirement:** Do not use `partial` classes, structs, or methods. Each type must be fully defined in a single file. This improves code discoverability and ensures the complete type definition is visible in one place.

**Severity:** High

**Exceptions:**

- Files inside `Generated/` folders are exempt — machine-generated code is permitted to use `partial`.
- Hand-authored partial declarations are also exempt **when the declared type name matches a partial type declared in any `Generated/` file in the source tree**. The hand-authored files may be named anything and live anywhere (e.g. `Builders/BaseExtensionClass_Logic.cs`, `Builders/BaseExtensionClass_Hooks.cs`); only the generated partial must be inside a `Generated/` folder. This covers patterns such as extensions split across a `Builders/` directory and its `Builders/Generated/` subdirectory.
- Hand-authored partial declarations are also exempt **when the declared type name matches a partial type generated into `obj/` at build time**. This covers NSwag and `<OpenApiReference>` generated API clients — the generated files are not committed but are detected at analysis time so that hand-authored partial companions (e.g. `Schema/IPatientApiClient.cs` extending a generated `IPatientApiClient`) are not flagged.
- All other use of `partial` is banned, including source-generator patterns such as `[LoggerMessage]` and `[GeneratedRegex]` — use `ILogger` extension methods directly instead.

### Logging Pattern

Use `ILogger` methods directly instead of `[LoggerMessage]` source-generated partial methods:

✅ **Good:**

```csharp
public sealed class UserService
{
    private readonly ILogger<UserService> _logger;

    public UserService(ILogger<UserService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ProcessAsync(Guid userId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing user {UserId}", userId);
        // ...
        _logger.LogWarning("User {UserId} not found", userId);
    }
}
```

❌ **Bad:**

```csharp
// Requires partial class — banned
public sealed partial class UserService
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Processing user {UserId}")]
    private partial void LogProcessing(Guid userId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "User {UserId} not found")]
    private partial void LogUserNotFound(Guid userId);
}
```

---

## CS-003: No Magic Numbers

**Type:** Guardrail

**Requirement:** Do not use numeric or string literals to represent domain concepts. Define enums for status codes, category identifiers, type discriminators, and similar domain values. When comparing against a domain value, always use a named constant or enum member — never a raw literal with a comment explaining what it means.

**Severity:** Medium

**Exceptions:**

- Numeric literals `0` and `1` in arithmetic or loop contexts (e.g., `i = 0; i < count; i++`).
- String literals for log messages, error messages, and display text.
- Numeric literals in entity configuration (e.g., `.HasMaxLength(256)`).
- Constants in test assertions where the value is the expected result.

✅ **Good:**

```csharp
// Define an enum
public enum UserStatus
{
    Active = 1,
    Inactive = 2,
    Suspended = 3
}

// Use the enum in comparisons
if (user.Status != UserStatus.Active)
{
    return Error("User is not active");
}

// Or use a strongly-typed property
if (!user.IsActive)
{
    return Error("User is not active");
}
```

❌ **Bad:**

```csharp
// Magic number with comment — the comment IS the problem
if (user.StatusId != 1) // 1 = Active
{
    return Error("User is not active");
}

// Magic string for type discrimination
if (claim.Type == "org_admin")
{
    // ...
}
```

---

## CS-004: Global Usings

**Type:** Steer

**Requirement:** Use global using directives (in a `GlobalUsings.cs` or `Usings.cs` file) for namespaces that are used across the majority of files in a project. Do not repeat `using` directives in individual files when they are already declared globally. When adding a new file, check the project's global usings before adding `using` statements.

**Severity:** Medium

**Evidence Required:** State which global usings exist in the project and confirm no individual files duplicate them. When adding new files, confirm the `using` directives are not already covered by global usings.

### Common Global Usings

```csharp
// GlobalUsings.cs
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;
global using Microsoft.Extensions.Logging;
global using MediatR;
```

✅ **Good:**

```csharp
// File has no using for System.Linq because it's in global usings
namespace MyService.Domain.Handlers;

public class GetUsersHandler : IRequestHandler<GetUsersQuery, List<UserDto>>
{
    // Can use LINQ methods without a local using
}
```

❌ **Bad:**

```csharp
// Duplicates global usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MyService.Domain.Handlers;
```

---

## CS-005: Explicit Private Readonly Fields

**Type:** Guardrail

**Requirement:** All constructor-injected dependencies must be assigned to `private readonly` fields. Every field assignment must include a null guard (`?? throw new ArgumentNullException(nameof(parameterName))`). This applies whether using primary constructors or traditional constructors. Never use constructor parameters directly in method bodies without assigning them to fields first.

**Severity:** High

**Exceptions:**

- **Record types** (`record`, `record class`, `record struct`) are excluded — positional parameters automatically become public properties. Records are intended for immutable data carriers (commands, queries, events, identifiers, results), not service classes with injected dependencies.
- **Exception types** (classes inheriting from `Exception` or any `*Exception` base class) are excluded — constructor parameters are forwarded to the base class, not stored as fields.
- **Base constructor forwarded parameters** — when a primary constructor forwards a parameter directly to a base constructor (e.g., `class Foo(ISettings settings) : BaseClass(settings)`), that parameter does not need to be stored as a private readonly field. Only parameters that are used in the class body require field assignment.
- Value types (int, bool, struct) and non-nullable records/strings that cannot be null do not need null guards, but must still be assigned to `private readonly` fields.
- `IOptions<T>` and similar framework-provided wrappers may be unwrapped in the constructor (e.g., `_settings = options.Value`).
- Field type names may use namespace-qualified forms (e.g., `Serilog.ILogger`) — the analyser accepts dots in type names.

**Additional constraint:** Constructors must not perform async I/O, long-running operations, or any work beyond assigning fields. Complex initialisation should be moved to a factory method or an `InitialiseAsync()` pattern. Long-running constructor work makes debugging difficult and delays dependency injection container resolution.

✅ **Good (primary constructor):**

```csharp
public sealed class UserService(
    IUserRepository repository,
    ILogger<UserService> logger)
{
    private readonly IUserRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly ILogger<UserService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _repository.GetByIdAsync(id, cancellationToken);
}
```

✅ **Good (traditional constructor):**

```csharp
public sealed class UserService
{
    private readonly IUserRepository _repository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository repository, ILogger<UserService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _repository.GetByIdAsync(id, cancellationToken);
}
```

❌ **Bad (primary constructor parameter used directly):**

```csharp
public sealed class UserService(
    IUserRepository repository,
    ILogger<UserService> logger)
{
    // No private readonly fields — parameters used directly
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await repository.GetByIdAsync(id, cancellationToken);
}
```

❌ **Bad (no null guard):**

```csharp
public sealed class UserService
{
    private readonly IUserRepository _repository;

    public UserService(IUserRepository repository)
    {
        _repository = repository; // No null check!
    }
}
```

✅ **Good (base constructor forwarding — forwarded param does not need a field):**

```csharp
// routeSettings is forwarded to BaseController; only repository needs a field
public sealed class UserController(
    IUserRepository repository,
    RouteSettings routeSettings) : BaseController(routeSettings)
{
    private readonly IUserRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
}
```

---

## CS-006: Method Complexity

**Type:** Guardrail

**Requirement:** Keep methods simple and focused. Methods should have low cyclomatic complexity — aim for 10 or fewer linearly independent paths. Methods with excessively complex branching logic must be decomposed into smaller, well-named private methods. Prioritise readability over cleverness.

**Severity:** Medium

**Exceptions:**

- Mapping methods that switch over many enum values may exceed the threshold where each case is trivial.
- Auto-generated code (e.g., EF Core migrations) is exempt.

✅ **Good:**

```csharp
public async Task<GreetingResult> ProcessGreetingAsync(
    Greeting greeting, CancellationToken cancellationToken)
{
    ValidateGreeting(greeting);
    var enriched = await EnrichWithContextAsync(greeting, cancellationToken);
    return await PersistAndNotifyAsync(enriched, cancellationToken);
}

private void ValidateGreeting(Greeting greeting)
{
    if (!greeting.IsActive)
        throw new GreetingInactiveException(greeting.Id);
}

private async Task<Greeting> EnrichWithContextAsync(
    Greeting greeting, CancellationToken cancellationToken)
{
    // Focused, single-purpose method
}
```

❌ **Bad:**

```csharp
public async Task<GreetingResult> ProcessGreetingAsync(
    Greeting greeting, CancellationToken cancellationToken)
{
    // 50+ lines with deeply nested if/else/switch/try-catch
    if (greeting != null)
    {
        if (greeting.IsActive)
        {
            if (greeting.Status == GreetingStatus.Pending)
            {
                try
                {
                    if (greeting.HasResponses)
                    {
                        foreach (var r in greeting.Responses)
                        {
                            if (r.IsValid)
                            {
                                // ... deeply nested logic continues
                            }
                        }
                    }
                }
                catch { /* swallowed */ }
            }
        }
    }
}
```

---

## CS-007: No Ref/Out Parameters

**Type:** Guardrail

**Requirement:** Do not use `ref` or `out` parameters in public or internal method signatures. If a method needs to return multiple values, use a tuple, a record, or a dedicated result type. `ref` and `out` force callers into a specific implementation pattern and make method contracts harder to understand.

**Severity:** Medium

**Exceptions:**

- Low-level performance-critical `private` methods where avoiding allocation is measured and justified.
- Interop with existing framework APIs that require `out` (e.g., `TryParse` pattern implementations).

✅ **Good:**

```csharp
// Return a result type
public record ParseResult(bool Success, Guid Value);

public ParseResult TryParseGreetingId(string input)
{
    if (Guid.TryParse(input, out var id))
        return new ParseResult(true, id);

    return new ParseResult(false, Guid.Empty);
}

// Or return a nullable
public Guid? TryParseGreetingId(string input)
    => Guid.TryParse(input, out var id) ? id : null;
```

❌ **Bad:**

```csharp
// out parameter on a public method
public bool TryGetGreeting(Guid id, out Greeting greeting)
{
    greeting = _repository.GetById(id);
    return greeting != null;
}

// ref parameter
public void UpdateGreeting(ref Greeting greeting, string newName)
{
    greeting.Name = newName;
}
```

---

## CS-008: No Member Hiding

**Type:** Guardrail

**Requirement:** Do not use the `new` modifier to hide members inherited from a base type. Member hiding breaks polymorphism and creates confusing behaviour where the result depends on whether the reference is cast as the base or derived type. If the compiler warns about hiding, it is a sign the design needs re-evaluating.

**Severity:** High

**Exceptions:** None.

✅ **Good:**

```csharp
public class SpecialisedGreetingService : GreetingService
{
    // Override virtual method instead of hiding
    public override string FormatGreeting(string name)
        => $"Dear {name}, welcome!";
}
```

❌ **Bad:**

```csharp
public class SpecialisedGreetingService : GreetingService
{
    // Hides base class method — different behaviour depending on cast
    public new string FormatGreeting(string name)
        => $"Dear {name}, welcome!";
}
```

---

## CS-009: No Regions

**Type:** Guardrail

**Requirement:** Do not use `#region` / `#endregion` directives. Regions hide code complexity — if a type needs regions to be readable, it is too complex and should be decomposed into smaller types. Collapsing code behind regions discourages developers from understanding the full scope of a type.

**Severity:** Medium

**Exceptions:** None.

✅ **Good:**

```csharp
// Small, focused class — no regions needed
public sealed class GreetingService(IGreetingRepository repository)
{
    private readonly IGreetingRepository _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));

    public async Task<Greeting?> GetByIdAsync(Guid id, CancellationToken ct)
        => await _repository.GetByIdAsync(id, ct);

    public async Task CreateAsync(Greeting greeting, CancellationToken ct)
        => await _repository.AddAsync(greeting, ct);
}
```

❌ **Bad:**

```csharp
public class GreetingService
{
    #region Fields
    private readonly IGreetingRepository _repository;
    private readonly ILogger<GreetingService> _logger;
    #endregion

    #region Constructor
    public GreetingService(IGreetingRepository repository, ILogger<GreetingService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    #endregion

    #region Public Methods
    // ...
    #endregion

    #region Private Methods
    // ...
    #endregion
}
```

---

## CS-010: Namespace-Folder Alignment

**Type:** Guardrail

**Requirement:** The namespace of a type must match the folder path relative to the project root. The project's root namespace forms the base, and each subfolder adds a segment. This ensures types are discoverable by navigating the folder structure.

**Severity:** High

**Exceptions:**

- Test projects may use a flat namespace matching the class under test's namespace.

✅ **Good:**

```
src/
  MyService.Domain/
    Aggregates/
      Greeting.cs               → namespace MyService.Domain.Aggregates
    Interfaces/
      IGreetingRepository.cs    → namespace MyService.Domain.Interfaces
  MyService.Infrastructure/
    Repositories/
      GreetingRepository.cs     → namespace MyService.Infrastructure.Repositories
```

❌ **Bad:**

```csharp
// File is in Repositories/ folder but namespace doesn't match
// File: Infrastructure/Repositories/GreetingRepository.cs
namespace MyService.Infrastructure.DataAccess; // Should be .Repositories
```

---

## CS-011: Explicit Access Modifiers

**Type:** Guardrail

**Requirement:** Always state access modifiers explicitly on all type and member declarations. Never rely on C# defaults (`internal` for types, `private` for members). Explicit modifiers make intent clear and improve readability.

**Severity:** Medium

**Exceptions:** None.

✅ **Good:**

```csharp
public sealed class GreetingService
{
    private readonly IGreetingRepository _repository;

    public GreetingService(IGreetingRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    internal async Task NotifyAsync(Guid id, CancellationToken ct) { }

    private void ValidateInput(string name) { }
}
```

❌ **Bad:**

```csharp
// Missing explicit access modifiers — relies on defaults
class GreetingService  // Implicitly internal
{
    readonly IGreetingRepository _repository;  // Implicitly private — but not stated

    void ValidateInput(string name) { }        // Implicitly private — but not stated
}
```

---

## CS-012: No Dynamic Keyword

**Type:** Guardrail

**Requirement:** Do not use the `dynamic` keyword. It bypasses compile-time type checking, causes serious performance issues, and makes refactoring hazardous. Use generics, interfaces, or pattern matching instead. EMIS-X microservices have no COM interop or dynamic runtime scenarios that would justify its use.

**Severity:** High

**Exceptions:** None.

✅ **Good:**

```csharp
// Use generics for flexible typing
public T Deserialise<T>(string json) where T : class
    => JsonSerializer.Deserialize<T>(json)
        ?? throw new SerialisationException($"Failed to deserialise {typeof(T).Name}");

// Use pattern matching for type discrimination
if (response is SuccessResult success)
{
    return success.Value;
}
```

❌ **Bad:**

```csharp
// dynamic bypasses all compile-time checks
dynamic result = GetResponse();
var value = result.SomeProperty;  // No compile-time safety

// dynamic in method signatures
public dynamic ProcessRequest(dynamic input) => input.Value;
```

---

## CS-013: Self-Documenting Code

**Type:** Steer

**Requirement:** Write code that is self-documenting through clear naming, small focused methods, and meaningful type names. Comments should only be used where code genuinely cannot express intent (e.g., explaining a non-obvious business rule or a workaround for a known upstream issue). Never leave commented-out code in the codebase — use source control to track history instead.

**Severity:** Medium

**Evidence Required:** Confirm that any comments in generated code explain _why_, not _what_. Confirm no commented-out code is present.

✅ **Good:**

```csharp
// Business rule: NHS Spine requires the ERN prefix for organisation lookups
var lookupKey = $"urn:emis:org:{organisationId}";

// Clear naming removes the need for comments
public async Task<IReadOnlyList<ActiveGreeting>> GetActiveGreetingsForOrganisationAsync(
    Guid organisationId, CancellationToken cancellationToken)
{
    return await _repository.GetByOrganisationAsync(organisationId, cancellationToken);
}
```

❌ **Bad:**

```csharp
// Get the greetings  ← comment restates the code
var greetings = await _repository.GetAllAsync(ct);

// Filter active ones  ← comment restates the code
var active = greetings.Where(g => g.IsActive);

// TODO: fix this later
// var oldResult = _legacyService.GetGreetings(id);
// if (oldResult != null) return oldResult;
```

---

## CS-014: No Logging Level Guards

**Type:** Guardrail

**Requirement:** Do not wrap `ILogger` calls in `if (_logger.IsEnabled(LogLevel.*))` guards. The `ILogger.Log*` extension methods already check the log level internally and short-circuit when the level is disabled. The guard adds visual noise, increases indentation, and provides no performance benefit when using structured logging placeholders (`{UserId}`).

Log level filtering should be configured via `appsettings.json` or environment variables — not via conditional checks in code.

**Severity:** Medium

**Exceptions:**

- When the log message requires **expensive computation** to produce an argument (e.g. serialising an object to JSON). In this case, add a `// guardrail:skip=CS-014:expensive argument evaluation` comment.
- Test classes

✅ **Good:**

```csharp
_logger.LogDebug("Created user history record for user {UserId}", userId);
_logger.LogInformation("Processing request {RequestId}", requestId);
_logger.LogWarning("User {UserId} not found", userId);
```

❌ **Bad:**

```csharp
if (_logger.IsEnabled(LogLevel.Debug))
{
    _logger.LogDebug("Created user history record for user {UserId}", userId);
}

if (_logger.IsEnabled(LogLevel.Information))
{
    _logger.LogInformation("Processing request {RequestId}", requestId);
}
```

---

## CS-015: .NET Analysers Enabled

**Type:** Guardrail

**Requirement:** The `src/Directory.Build.props` file must enable the Microsoft .NET code analysers with warnings treated as errors. All four of the following MSBuild properties must be present and set to the specified values:

| Property                  | Required Value       | Purpose                                                          |
| ------------------------- | -------------------- | ---------------------------------------------------------------- |
| `EnableNETAnalyzers`      | `true`               | Activates the Microsoft.CodeAnalysis.NetAnalyzers analyser pack  |
| `TreatWarningsAsErrors`   | `true`               | Promotes all warnings (including analyser diagnostics) to errors |
| `EnforceCodeStyleInBuild` | `true`               | Enforces IDE code style rules (IDExxxx) during `dotnet build`    |
| `AnalysisLevel`           | `latest-recommended` | Uses the latest recommended rule set for the target framework    |

These properties ensure that code quality rules are enforced consistently across all projects without relying on individual developers having the correct IDE settings.

**Severity:** High

**Exceptions:** None.

✅ **Good:**

```xml
<!-- src/Directory.Build.props -->
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>13</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
  </PropertyGroup>
</Project>
```

❌ **Bad (analysers disabled):**

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <EnableNETAnalyzers>false</EnableNETAnalyzers>
  </PropertyGroup>
</Project>
```

❌ **Bad (missing properties):**

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <!-- No analyser properties at all -->
  </PropertyGroup>
</Project>
```

❌ **Bad (wrong analysis level):**

```xml
<Project>
  <PropertyGroup>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest</AnalysisLevel>  <!-- Should be latest-recommended -->
  </PropertyGroup>
</Project>
```

---

## CS-016: Dead Code Removal

**Type:** Steer

**Requirement:** After modifying or refactoring code, identify and remove any methods, classes, properties, interfaces, fields, or parameters that are no longer referenced. When a change renders existing code unreachable or unused, that dead code must be removed in the same changeset — not left behind for a future cleanup.

This applies to all refactoring scenarios including:

- Removing calls to a method — delete the method if no other callers remain
- Changing a class hierarchy — remove base class members that subclasses no longer invoke
- Replacing a mechanism — delete the old implementation entirely
- Removing a feature — delete all supporting types, tests, and configuration

**Severity:** High

**Evidence Required:** When completing a change, state which code paths were checked for dead code, and confirm that no unused members remain. If dead code was found and removed, list the removed members. If the changeset modifies or deletes callers of a method, explicitly verify that the method still has at least one remaining caller before leaving it in place.

**Why this matters:** AI agents are effective at implementing the requested change but frequently fail to trace the ripple effects — leaving behind defunct methods, orphaned helper classes, and unused parameters. Over time this accumulates into a codebase littered with dead code that misleads developers, confuses future AI agents, and inflates maintenance cost.

✅ **Good — method removed when its only caller was deleted:**

```csharp
// Before: base class had a validation method called by subclasses
public abstract class ApplicationCommandValidator<T> : AbstractValidator<T>
    where T : ApplicationCommand
{
    protected void ValidateName() { /* ... */ }
    protected void ValidateMigrationUrl() { /* ... */ }
}

// After: MigrationUrl validation was moved elsewhere.
// ValidateHasMigrationUrl() is no longer called by any subclass,
// so it was removed from the base class.
public abstract class ApplicationCommandValidator<T> : AbstractValidator<T>
    where T : ApplicationCommand
{
    protected void ValidateName() { /* ... */ }
    // ValidateMigrationUrl() removed — no remaining callers
}
```

❌ **Bad — defunct method left behind after refactoring:**

```csharp
// The agent moved MigrationUrl validation into each subclass directly,
// but left the base class method in place with zero callers.
public abstract class ApplicationCommandValidator<T> : AbstractValidator<T>
    where T : ApplicationCommand
{
    protected void ValidateName() { /* ... */ }

    // Dead code — no subclass calls this any more
    protected void ValidateHasMigrationUrl()
    {
        RuleFor(command => command.MigrationUrl)
            .NotEmpty()
            .WithMessage("MigrationUrl is required");
    }
}
```

---

## CS-017: Inject TimeProvider

**Type:** Guardrail

**Requirement:** Never call `DateTime.UtcNow`, `DateTime.Now`, `DateTimeOffset.UtcNow`, or `DateTimeOffset.Now` directly. Inject `TimeProvider` (the .NET 8+ framework abstraction in `System`) and call `timeProvider.GetUtcNow()` instead. Register `TimeProvider.System` as a singleton in production DI. In unit tests, use `Microsoft.Extensions.TimeProvider.Testing.FakeTimeProvider` or mock `TimeProvider` to control the clock.

**Severity:** High

**Exceptions:** Test helper code that generates JWT tokens (e.g., `MockTokenGenerator`) may use `DateTime.UtcNow` for token expiry because the token is consumed within the same test run and testability of the clock is not a concern.

**Why this matters:** Static `DateTime.UtcNow` calls create an untestable seam — tests cannot control or assert the exact timestamp, leading to flaky time-dependent assertions and an inability to verify audit fields. `TimeProvider` is the framework-standard solution since .NET 8, replacing the need for custom `IClock` or `IDateTimeProvider` interfaces.

✅ **Good:**

```csharp
public class DeleteRecordingCommandHandler(
    IRecordingsMetadataRepository repository,
    TimeProvider timeProvider,
    ILogger<DeleteRecordingCommandHandler> logger)
    : IRequestHandler<DeleteRecordingCommand, Unit>
{
    private readonly IRecordingsMetadataRepository _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));
    private readonly TimeProvider _timeProvider = timeProvider
        ?? throw new ArgumentNullException(nameof(timeProvider));

    public async Task<Unit> Handle(DeleteRecordingCommand request, CancellationToken cancellationToken)
    {
        var deletedAt = _timeProvider.GetUtcNow().UtcDateTime;
        metadata.DeletedAt = deletedAt;
        // ...
    }
}

// DI registration
services.AddSingleton(TimeProvider.System);

// Unit test
var fakeTime = new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);
var mockTimeProvider = new Mock<TimeProvider>();
mockTimeProvider.Setup(tp => tp.GetUtcNow()).Returns(fakeTime);
// Assert exact timestamp
Assert.Equal(fakeTime.UtcDateTime, metadata.DeletedAt);
```

❌ **Bad:**

```csharp
public async Task<Unit> Handle(DeleteRecordingCommand request, CancellationToken cancellationToken)
{
    // Untestable — test cannot control or assert the exact timestamp
    metadata.DeletedAt = DateTime.UtcNow;
    // ...
}
```

---

## Gotchas

- Primary constructors in C# 13 capture their parameters as hidden fields. If you use them directly in method bodies (e.g., `repository.GetAsync(...)` instead of `_repository.GetAsync(...)`), the code compiles and works — but it bypasses the null guard and the explicit `private readonly` field, violating CS-005 silently.
- `[LoggerMessage]` source generators require `partial` classes. Since partial classes are banned (CS-002), use `ILogger.LogInformation(...)` directly. The performance difference is negligible for the log levels EMIS-X services use.
- The `dynamic` keyword compiles and runs fine in simple cases, which is why agents default to it for deserialization. Always use `JsonSerializer.Deserialize<T>()` or pattern matching instead — `dynamic` is banned (CS-012) because it disables compile-time safety and significantly degrades performance.
- `#region` blocks are a code smell detector. If you feel the urge to add regions, the class is too large — split it. The guardrail (CS-009) exists because regions hide complexity rather than reducing it.
- Agents default to `DateTime.UtcNow` for timestamps because it dominates training data. EMIS-X requires `TimeProvider` injection instead — `DateTime.UtcNow` is banned (CS-017) because it creates an untestable seam.

---

## Critical Reminders

- One type per file, filename matches type name — no partial classes, no multi-type files (CS-001, CS-002)
- Inject dependencies through primary constructors into private readonly fields; never capture the DI parameter directly in method bodies (CS-005)
- Keep methods under 30 statements and cyclomatic complexity under 10 — extract private methods or separate handlers when logic grows (CS-006)
- Namespace must mirror the folder path from project root; moving a file means updating the namespace (CS-010)
- No code comments — naming is the documentation; if a method needs a comment, rename it (CS-013)
- After every refactoring, trace the call graph of modified members and remove anything that lost all callers (CS-016)
- Inject `TimeProvider` for current time — never call `DateTime.UtcNow` or `DateTime.Now` directly; register `TimeProvider.System` in DI (CS-017)
