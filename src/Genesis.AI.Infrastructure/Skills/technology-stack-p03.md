# SKILL: technology-stack-p03
# Phase: P03 Architecture — Phase 1

## Technology Stack Confirmation

**Purpose:** Confirm the EMIS-X platform stack and capture ADRs for project-specific decisions. The core stack is mandated — these questions confirm alignment and surface any justified deviations.

**Fast-track rule:** When the ROUTING CONTEXT shows `stack_mandated: true` (always the case for EMIS-X), reduce to 2 essential questions only:
1. Database choice: Aurora Postgres 17 (`PG-001`) or DynamoDB (`DDB-*`)?
2. Auth provider: Which CIS2/Azure AD B2C variant for this project?

All other stack questions are answered by the mandate. Do All other stack questions are answered by the mandate. Do All other stack questions are answered by the mandate. Do All other stack questions are answered by the mandate. Do All other stack questions are answered by the mandate. Do All other stack questions are answered by the mandate. Do AlreAll other stack questions ar12.x (`ENG-*`). Any reason this project would deviate?"
2. "Confirming frontend: React 18.3+ single-spa microfrontend with pnpm (`WA-005`). Any reason to deviate?"
3. "Database — Aurora Postgres 17 (`PG-001`) or DynamoDB (`DDB-*`)?" → This IS a genuine choice. Validate selection against data model.
4. "API protocol: REST + JSON:API via `Emis.JsonApi` (`API-001`). Confirmed?"
5. "Authentication: CIS2 OAuth2 / Azure AD B2C → JWT claims (`AUTH-*`). Which provider for this project?"
6. "Hosting: ECS Fargate is standard. Any reason to use Lambda instead?"
7. "CI/CD: GitHub Actions. Confirmed?"
8. "Is this an EMIS-X microfrontend registered in the ACP shell?" → If YES: mandate `applicationDiscovery` field in `package.json` (`AD-001`).
9. "Is `@emisgroup/acp-security-headers` declared in `package.json`?" → Mandatory for all EMIS-X webapps (`WSEC-013`).
10. "Backend project structure confirmed as `{Service}.Api / .Core / .Domain / .Infrastructure`?" → Mandatory (`ARCH-*`).
11. "Flyway 11.x confirmed for all database migrations?" → Mandatory (`PG-001`).
12. Index strategy ADR — see `mandatory-adr-index-strategy` skill.
13. Idempotency key ADR — see `mandatory-adr-idempotency` skill.

## ADR Format

For each non-default decision, create a Decision Record:

```
**ADR-{NNN}: {Title}**
- Context: {Why needed}
- Decision: {Choice}
- Alternatives: {What else considered}
- Rationale: {Why}
- Consequences: {Trade-offs, downsides}
- EMIS Principle: {Which principle validated}
- Guardrail: {e.g. ENG-002, PG-001, API-001}
```

## Validation

"Stack confirmation:
- Backend: ASP.NET Core 10.0 + MediatR 12.x ✅
- Frontend: React 18.3+ single-spa ✅
- Database: {Postgres / DynamoDB} ✅
- API: REST + JSON:API ✅
- Auth: {provider} ✅
- ADRs created: {list}

Correct?"
