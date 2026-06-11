# SKILL: emis-x-stack-reference
# Phase: P03 Architecture — Phase 12

## EMIS-X Stack Reference (for ADR Citation)

Use these exact version references when creating ADRs. These are the mandated versions — do not cite general library names without versions.

### Backend Stack

| Component | Version | Guardrail(s) |
|-----------|---------|-------------|
| .NET / ASP.NET Core | 10.0 | — |
| C# Language | 13 | CS-001 |
| Entity Framework Core | 10.0 | DATA-001 |
| Npgsql EF Core Provider | 10.0 | PG-001 |
| MediatR | 12.x | ENG-002 |
| FluentValidation | 11.x | ENG-008 |
| AutoMapper | 13.x | CS-010 |
| xUnit | 3.x | TEST-001 |
| Moq | 4.20.x | TEST-001 |
| Swashbuckle | 9.0.x | API-012 |
| Emis.JsonApi | latest | API-001 |
| Testcontainers | 4.x | TEST-005 |

### Frontend Stack

| Component | Version | Guardrail(s) |
|-----------|---------|-------------|
| React | 18.3+ | WA-001 |
| TypeScript | 5.8+ | WCS-001 |
| single-spa | latest | WA-001 |
| pnpm | latest | SC-001 |
| axios | latest | HTTP-001 |

### Database & Infrastructure

| Component | Version | Guardrail(s) |
|-----------|---------|-------------|
| PostgreSQL | 17.x | PG-001 |
| Flyway | 11.x | PG-001 |
| Docker base image | mcr.microsoft.com/dotnet/aspnet:10.0 | OBS-001 |

### ADR Citation Format

When creating ADRs referencing the stack, use: `ASP.NET Core 10.0 (ENG-002, CS-001)` not just `ASP.NET Core`.
